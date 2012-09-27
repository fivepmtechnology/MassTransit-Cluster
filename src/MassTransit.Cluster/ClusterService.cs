using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Automatonymous;
using MassTransit.Cluster.Bully;
using MassTransit.Cluster.Configuration;
using MassTransit.Cluster.LamportMutex;
using MassTransit.Cluster.Messages;
using MassTransit.Logging;
using MassTransit.Util;

namespace MassTransit.Cluster
{
    public class ClusterService : IBusService, Consumes<IClusterMessage>.All
    {
        private readonly ClusterSettings _settings;

        internal ClusterSettings Settings
        {
            get { return _settings; }
        }

        private readonly IServiceBus _bus;
        internal IServiceBus Bus { get { return _bus; } }

        private readonly ILog _log = Logger.Get(typeof(ClusterService));

        private readonly uint[] _clock;

        internal ClusterService([NotNull] ClusterSettings settings, [NotNull] IServiceBus bus)
        {
            _settings = settings;

            _clock = new uint[settings.EndpointCount];

            _bus = bus.ControlBus;
        }

        void IDisposable.Dispose()
        {
            Stop();
        }

        private UnsubscribeAction[] _unsubscribeActions;

        /// <summary>
        /// Called when the service is being started, which is after the service bus has been started.
        /// </summary>
        /// <param name="bus">The service bus</param>
        public void Start(IServiceBus bus)
        {
            _unsubscribeActions = new[]
                                     {
                                         bus.SubscribeInstance(this),
                                         bus.SubscribeInstance(new LamportMutexHandler(this)),
                                         bus.SubscribeInstance(new BullyHandler(this)),
                                     };
        }

        /// <summary>
        /// Called when the ServiceBus is being disposed, to allow any resources or subscriptions to be released.
        /// </summary>
        public void Stop()
        {
            if (_unsubscribeActions != null)
            {
                foreach (var unsubscribeAction in _unsubscribeActions)
                    unsubscribeAction();

                _unsubscribeActions = null;
            }
        }

        public void Consume(IClusterMessage message)
        {
            lock (_clock)
            {
                for (uint idx = 0; idx < message.Clock.Length; idx++)
                    _clock[idx] = Math.Max(_clock[idx], message.Clock[idx]);
            }

        }
    }
}
