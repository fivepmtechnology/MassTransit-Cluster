using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MassTransit.Cluster.Configuration
{
	public interface IClusterConfigurator
	{
		void SetEndpointIndex(uint index);
		void SetEndpointCount(uint count);
		void SetHeartbeatInterval(TimeSpan interval);
		void SetElectionPeriod(TimeSpan period);
		void AddWonCoordinatorHandler(Action<IServiceBus> handler);
		void AddLostCoordinatorHandler(Action handler);
	}
}
