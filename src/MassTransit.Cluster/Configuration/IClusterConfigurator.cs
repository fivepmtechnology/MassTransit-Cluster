using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.BusConfigurators;

namespace MassTransit.Cluster.Configuration
{
	public interface IClusterConfigurator
	{
		void SetEndpointIndex(uint index);
		void SetHeartbeatInterval(TimeSpan interval);
		void SetElectionPeriod(TimeSpan period);
		void AddPromotionHandler(Action<IServiceBus> handler);
		void AddDemotionHandler(Action handler);

		ServiceBusConfigurator BusConfigurator { get; }
	}
}
