using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit.BusServiceConfigurators;
using Quartz;

namespace MassTransit.Cluster.Scheduler.Configuration
{
	internal class SchedulerConfigurator : BusServiceConfigurator
	{
		private readonly IScheduler _scheduler;

		public SchedulerConfigurator(IScheduler scheduler)
		{
			_scheduler = scheduler;
		}

		/// <summary>
		/// Creates the service
		/// </summary>
		/// <param name="bus"/>
		/// <returns>
		/// The instance of the service
		/// </returns>
		public IBusService Create(IServiceBus bus)
		{
			return new SchedulerService(_scheduler, bus);
		}

		/// <summary>
		/// Returns the type of the service created by the configurator
		/// </summary>
		public Type ServiceType { get { return typeof (SchedulerService); } }
		public BusServiceLayer Layer { get { return BusServiceLayer.Presentation; } }
	}
}
