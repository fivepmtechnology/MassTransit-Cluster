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
        void SetEndpointCount(uint count);
		void SetHeartbeatInterval(TimeSpan interval);
	}
}
