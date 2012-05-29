using System;
using System.Threading;
using MassTransit.NLogIntegration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit.Cluster.Configuration;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace MassTransit.Cluster.Tests
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			var evt1 = new ManualResetEventSlim(false);
			var evt2 = new ManualResetEventSlim(false);

			var bus1 = ServiceBusFactory.New(sbi =>
			{
				sbi.UseRabbitMq();
				sbi.UseRabbitMqRouting();
				sbi.ReceiveFrom("rabbitmq://localhost/clustertest-1");
				sbi.UseClusterService(cc =>
				{
					cc.SetEndpointIndex(1);
					cc.SetElectionPeriod(TimeSpan.FromSeconds(15));
					cc.AddWonCoordinatorHandler(bus => evt1.Set());
				});
				sbi.UseControlBus();
				sbi.UseNLog();
			});

			var bus2 = ServiceBusFactory.New(sbi =>
			{
				sbi.UseRabbitMq();
				sbi.UseRabbitMqRouting();
				sbi.ReceiveFrom("rabbitmq://localhost/clustertest-2");
				sbi.UseClusterService(cc =>
				{
					cc.SetEndpointIndex(2);
					cc.SetElectionPeriod(TimeSpan.FromSeconds(15));
					cc.AddWonCoordinatorHandler(bus => evt2.Set());
				});
				sbi.UseControlBus();
				sbi.UseNLog();
			});

			var result = WaitHandle.WaitAny(new[] { evt1.WaitHandle, evt2.WaitHandle }, TimeSpan.FromSeconds(30));
			Assert.IsTrue(result == 1, "Endpoint {0} is not highest endpoint but was elected leader anyway", result+1);
		}
	}
}
