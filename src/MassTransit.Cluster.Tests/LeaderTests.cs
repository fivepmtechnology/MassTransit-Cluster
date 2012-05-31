using System;
using System.Diagnostics;
using System.Threading;
using MassTransit.NLogIntegration;
using MassTransit.Cluster.Configuration;
using NLog;
using NUnit;
using NUnit.Framework;

namespace MassTransit.Cluster.Tests
{
	[TestFixture]
	public class LeaderTests
	{
		private Logger _log;
		private readonly LogFactory _logFactory = new LogFactory();

		[TestFixtureSetUp]
		public void HookupLogging()
		{
			_log = _logFactory.GetCurrentClassLogger();			
		}

		[Test]
		public void HighestLeaderElected()
		{
			var evt = new ManualResetEventSlim(false);

			const uint count = 5;
			for (uint i = count; i > 0; i--)
			{
				uint idx = i;
				var bus = ServiceBusFactory.New(sbi =>
				{
					sbi.UseMsmq();
					sbi.UseMulticastSubscriptionClient();
					sbi.ReceiveFrom("msmq://localhost/clustertest-" + idx.ToString());
					sbi.UseClusterService(cc =>
					{
						cc.SetEndpointIndex(idx);
						cc.SetElectionPeriod(TimeSpan.FromSeconds(15));
						cc.AddWonCoordinatorHandler(b =>
						{
							//_log.Info("#{0} elected as leader", idx);
							if(idx == count)
								evt.Set();
						});
					});
					sbi.EnableMessageTracing();
					sbi.UseNLog(_logFactory);
				});
			}

			var result = evt.Wait(TimeSpan.FromSeconds(30));
			Assert.IsTrue(result, "Highest endpoint was not elected leader");
		}

		[Test]
		public void LateLeaderShouldBully()
		{
			var evt = new ManualResetEventSlim(false);

			const uint count = 5;
			for (uint i = 0; i < count; i++)
			{
				uint idx = i;
				var bus = ServiceBusFactory.New(sbi =>
				{
					_log.Info("Configuring #{0}", idx);
					sbi.UseMsmq();
					sbi.UseMulticastSubscriptionClient();
					sbi.ReceiveFrom("msmq://localhost/clustertest-" + idx);
					sbi.UseClusterService(cc =>
					{
						cc.SetEndpointIndex(idx);
						cc.SetElectionPeriod(TimeSpan.FromSeconds(15));
						cc.AddWonCoordinatorHandler(b =>
						{
							_log.Info("#{0} elected as leader", idx);
						});
					});
					sbi.UseNLog(_logFactory);
				});
			}

			_log.Info("Waiting for bus to settle");

			Thread.Sleep(20);

			_log.Info("Introducing new hopeful leader #{0}", count);
			var newbus = ServiceBusFactory.New(sbi =>
			{
				sbi.UseMsmq();
				sbi.UseMulticastSubscriptionClient();
				sbi.ReceiveFrom("msmq://localhost/clustertest-" + count);
				sbi.UseClusterService(cc =>
				{
					cc.SetEndpointIndex(count);
					cc.SetElectionPeriod(TimeSpan.FromSeconds(15));
					cc.AddWonCoordinatorHandler(b =>
					{
						_log.Info("#{0} elected as leader", count);
						evt.Set();
					});
				});
				sbi.UseNLog(_logFactory);
			});

			var result = evt.Wait(TimeSpan.FromSeconds(30));
			Assert.IsTrue(result, "Highest endpoint was not elected leader");
		}

		[Test]
		public void LateLowerEndpointShouldNotWin()
		{
			var evt = new ManualResetEventSlim(false);

			const uint count = 5;
			for (uint i = 0; i < count; i++)
			{
				uint idx = i;
				var bus = ServiceBusFactory.New(sbi =>
				{
					sbi.UseMsmq();
					sbi.UseMulticastSubscriptionClient();
					sbi.ReceiveFrom("msmq://localhost/clustertest-" + idx.ToString());
					sbi.UseClusterService(cc =>
					{
						if(idx <= 3)
							cc.SetEndpointIndex(idx);
						else
							cc.SetEndpointIndex(idx+1);
						cc.SetElectionPeriod(TimeSpan.FromSeconds(15));
						cc.AddWonCoordinatorHandler(b =>
						{
							_log.Info("#{0} elected as leader", idx);
						});
					});
					sbi.EnableMessageTracing();
					sbi.UseNLog(_logFactory);
				});
			}

			Thread.Sleep(20);

			var newbus = ServiceBusFactory.New(sbi =>
			{
				sbi.UseMsmq();
				sbi.UseMulticastSubscriptionClient();
				sbi.ReceiveFrom("msmq://localhost/clustertest-" + count);
				sbi.UseClusterService(cc =>
				{
					cc.SetEndpointIndex(4);
					cc.SetElectionPeriod(TimeSpan.FromSeconds(15));
					cc.AddWonCoordinatorHandler(b =>
					{
						_log.Info("#{0} elected as leader", 4);
						evt.Set();
					});
				});
				sbi.EnableMessageTracing();
				sbi.UseNLog(_logFactory);
			});

			var result = evt.Wait(TimeSpan.FromSeconds(30));
			Assert.IsFalse(result, "New endpoint stole the leader");
		}

		[Test]
		public void DeadLeaderShouldBeReplaced()
		{
			bool set = false;
			var evt = new ManualResetEventSlim(false);

			const uint count = 5;
			for (uint i = 0; i < count; i++)
			{
				uint idx = i;
				var bus = ServiceBusFactory.New(sbi =>
				{
					sbi.UseMsmq();
					sbi.UseMulticastSubscriptionClient();
					sbi.ReceiveFrom("msmq://localhost/clustertest-" + idx.ToString());
					sbi.UseClusterService(cc =>
					{
						cc.SetEndpointIndex(idx);
						cc.SetElectionPeriod(TimeSpan.FromSeconds(5));
						cc.SetHeartbeatInterval(TimeSpan.FromSeconds(10));
						cc.AddWonCoordinatorHandler(b =>
						{
							_log.Info("#{0} elected as leader", idx);
							if(idx == count - 1 && set)
								evt.Set();
						});
					});
					sbi.EnableMessageTracing();
					sbi.UseNLog(_logFactory);
				});
			}

			var newbus = ServiceBusFactory.New(sbi =>
			{
				sbi.UseMsmq();
				sbi.UseMulticastSubscriptionClient();
				sbi.ReceiveFrom("msmq://localhost/clustertest-" + count);
				sbi.UseClusterService(cc =>
				{
					cc.SetEndpointIndex(count);
					cc.SetElectionPeriod(TimeSpan.FromSeconds(5));
					cc.SetHeartbeatInterval(TimeSpan.FromSeconds(10));
					cc.AddWonCoordinatorHandler(b =>
					{
						_log.Info("#{0} elected as leader", count);
					});
				});
				sbi.EnableMessageTracing();
				sbi.UseNLog(_logFactory);
			});

			Thread.Sleep(10);

			newbus.Dispose();

			var result = evt.Wait(TimeSpan.FromSeconds(60));
			Assert.IsTrue(result, "Highest remaining endpoint did not become elected leader");
		}
	}
}
