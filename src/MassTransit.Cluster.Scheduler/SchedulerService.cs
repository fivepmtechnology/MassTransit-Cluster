using System;
using System.Threading;
using Magnum.StateMachine;
using MassTransit.Logging;
using MassTransit.Util;
using Quartz;
using Quartz.Spi;

namespace MassTransit.Cluster.Scheduler
{
    public class SchedulerService : IBusService, IClusterService
    {
        private readonly Func<IScheduler> _schedulerFactory;

        private readonly IServiceBus _bus;
        private readonly ILog _log = Logger.Get(typeof(SchedulerService));
    	private UnsubscribeAction _unsubscribe;
    	private IScheduler _scheduler;

    	internal SchedulerService([NotNull] Func<IScheduler> schedulerFactory, [NotNull] IServiceBus bus)
        {
            _schedulerFactory = schedulerFactory;

            _bus = bus.ControlBus;
        }

        void IDisposable.Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Called when the service is being started, which is after the service bus has been started.
        /// </summary>
        /// <param name="bus">The service bus</param>
        public void Start(IServiceBus bus)
        {
        	_scheduler = _schedulerFactory();
			bus.SubscribeConsumer(() => new TimeoutHandlers(_scheduler));
        }

        /// <summary>
        /// Called when the ServiceBus is being disposed, to allow any resources or subscriptions to be released.
        /// </summary>
        public void Stop()
        {
        	if(_scheduler != null && !_scheduler.IsShutdown) _scheduler.Shutdown();
        }

		private class ServiceBusJobFactory : IJobFactory
		{
			private readonly IServiceBus _bus;

			public ServiceBusJobFactory(IServiceBus bus)
			{
				_bus = bus;
			}

			public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
			{
				var type = bundle.JobDetail.JobType;
				var ci = type.GetConstructor(new[] {typeof (IServiceBus)});
				if (ci != null)
					return (IJob) ci.Invoke(new object[] {_bus});

				ci = type.GetConstructor(new Type[0]);
				if(ci != null)
					return (IJob)ci.Invoke(new object[0]);

				throw new InvalidOperationException("Unable to construct job type");
			}
		}

    	public void Promoted(IServiceBus bus)
    	{
			if (_scheduler != null && !_scheduler.IsShutdown) _scheduler.Shutdown();
			_scheduler = _schedulerFactory();
			_scheduler.JobFactory = new ServiceBusJobFactory(bus);
    		_scheduler.Start();
    	}

    	public void Demoted()
    	{
    		_scheduler.Standby();
    	}
    }
}
