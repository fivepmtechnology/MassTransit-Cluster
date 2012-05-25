using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MassTransit.Cluster.Messages
{
	public class WantsMaster : IClusterMessage
	{
		public int EndpointIndex { get; set; }
	}
}
