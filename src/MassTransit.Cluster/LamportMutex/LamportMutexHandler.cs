using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.Cluster.LamportMutex.Messages;

namespace MassTransit.Cluster.LamportMutex
{
    public class LamportMutexHandler : Consumes<Request>.Context
    {
        private readonly ClusterService _clusterService;

        public LamportMutexHandler(ClusterService clusterService)
        {
            _clusterService = clusterService;
        }

        public void Consume(IConsumeContext<Request> message)
        {
            
        }
    }
}
