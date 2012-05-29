using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MassTransit.Cluster.Messages
{
	public class Okay : IClusterMessage
	{
		public uint SourceIndex { get; set; }
	}
}
