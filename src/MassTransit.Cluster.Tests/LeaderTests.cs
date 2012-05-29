using System;
using System.Diagnostics;
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
	public class LeaderTests
	{
		[TestMethod]
		public void HighestLeaderElected()
		{
			var evt = new ManualResetEventSlim(false);

			const int count = 5;
			for (int i = 0; i < count; i++)
			{
				int idx = i;
				var bus = ServiceBusFactory.New(sbi =>
				{
					sbi.UseMsmq();
					sbi.UseMulticastSubscriptionClient();
					sbi.ReceiveFrom("msmq://localhost/clustertest-" + idx.ToString());
					sbi.UseClusterService(cc =>
					{
						cc.SetEndpointIndex(1);
						cc.SetElectionPeriod(TimeSpan.FromSeconds(15));
						cc.AddWonCoordinatorHandler(b =>
						{
							Debug.WriteLine("#{0} elected as leader", idx);
							if(idx == count-1)
								evt.Set();
						});
					});
					sbi.EnableMessageTracing();
					sbi.UseNLog();
				});
			}

			var result = evt.Wait(TimeSpan.FromSeconds(30));
			Assert.IsTrue(result, "Highest endpoint was not elected leader");
		}
	}
}
