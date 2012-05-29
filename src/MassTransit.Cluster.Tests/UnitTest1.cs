using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MassTransit.Cluster.Configuration;

namespace MassTransit.Cluster.Tests
{
	[TestClass]
	public class UnitTest1
	{
		[TestMethod]
		public void TestMethod1()
		{
			var bus = ServiceBusFactory.New(sbi =>
			{
				sbi.UseRabbitMq();
				sbi.UseRabbitMqRouting();
				sbi.ReceiveFrom("rabbitmq://localhost/clustertest-1");
				sbi.UseClusterService(cc =>
				{
					cc.SetEndpointIndex(1);
				});
			});
		}
	}
}
