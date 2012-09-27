using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.Cluster.Messages;

namespace MassTransit.Cluster.LamportMutex.Messages
{
    public class Request : IClusterMessage
    {
        public uint SourceIndex { get; set; }
        public uint[] Clock { get; set; }
    }
}
