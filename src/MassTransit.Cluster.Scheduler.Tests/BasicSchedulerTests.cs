using System;
using System.Threading;
using MassTransit.Cluster.Configuration;
using MassTransit.Cluster.Scheduler.Configuration;
using MassTransit.NLogIntegration;
using MassTransit.Services.Timeout.Messages;
using NLog;
using NUnit.Framework;
using Quartz.Impl;

namespace MassTransit.Cluster.Scheduler.Tests
{
	[TestFixture]
	public class BasicSchedulerTests
	{
		private Logger _log;
		private readonly LogFactory _logFactory = new LogFactory();

		[TestFixtureSetUp]
		public void HookupLogging()
		{
			_log = _logFactory.GetCurrentClassLogger();
		}

		[Test]
		public void TestMethod1()
		{
			var quartzBuilder = new StdSchedulerFactory();
			quartzBuilder.Initialize();

			var bus = ServiceBusFactory.New(sbi =>
			{
				sbi.UseMsmq();
				sbi.UseMulticastSubscriptionClient();
				sbi.ReceiveFrom("msmq://localhost/clustertest-1");
				sbi.UseClusterService(cc =>
				{
					cc.SetEndpointIndex(1);
					cc.SetElectionPeriod(TimeSpan.FromSeconds(5));
					cc.UseScheduler(quartzBuilder.GetScheduler);
				});
				sbi.EnableMessageTracing();
				sbi.UseNLog(_logFactory);
			});

			var evt = new ManualResetEventSlim(false);
			bus.SubscribeHandler<TimeoutExpired>(m =>
			{
				evt.Set();
			});

			Thread.Sleep(5000);

			bus.Publish(new ScheduleTimeout(Guid.Empty, TimeSpan.FromSeconds(10)));
	
			Assert.IsTrue(evt.Wait(TimeSpan.FromSeconds(30)), "Didn't get a timeout expired message");
		}
	}
}
