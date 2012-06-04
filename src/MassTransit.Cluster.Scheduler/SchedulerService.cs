using System;
using System.Threading;
using Magnum.StateMachine;
using MassTransit.Logging;
using MassTransit.Util;
using Quartz;

namespace MassTransit.Cluster.Scheduler
{
    public class SchedulerService : IBusService
    {
        private readonly IScheduler _scheduler;

        private readonly IServiceBus _bus;
        private readonly ILog _log = Logger.Get(typeof(SchedulerService));
    	private UnsubscribeAction _unsubscribe;

    	internal SchedulerService([NotNull] IScheduler scheduler, [NotNull] IServiceBus bus)
        {
            _scheduler = scheduler;

            _bus = bus.ControlBus;
        }

        void IDisposable.Dispose()
        {
            Stop();
        }

        #region IServiceBus Implementation

        /// <summary>
        /// Called when the service is being started, which is after the service bus has been started.
        /// </summary>
        /// <param name="bus">The service bus</param>
        public void Start(IServiceBus bus)
        {
            _unsubscribe = bus.SubscribeConsumer(() => new TimeoutHandlers(_scheduler));
        }

        /// <summary>
        /// Called when the ServiceBus is being disposed, to allow any resources or subscriptions to be released.
        /// </summary>
        public void Stop()
        {
        	_scheduler.Standby();
        }

        #endregion
    }
}
