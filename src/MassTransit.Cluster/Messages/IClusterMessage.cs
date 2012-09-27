using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MassTransit.Cluster.Messages
{
    public interface IClusterMessage
	{
        uint SourceIndex { get; }
	    uint[] Clock { get; }
	}
}
