using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MassTransit.Cluster.Messages
{
	interface IClusterMessage
	{
		int EndpointIndex { get; }
	}
}
