using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit.BusServiceConfigurators;
using MassTransit.Cluster.Configuration;
using Quartz;

namespace MassTransit.Cluster.Scheduler.Configuration
{
	internal class SchedulerConfigurator : IClusterServiceConfigurator
	{
		private readonly Func<IScheduler> _schedulerFactory;

		public SchedulerConfigurator(Func<IScheduler> schedulerFactory)
		{
			_schedulerFactory = schedulerFactory;
		}

		public IClusterService Create(IServiceBus bus)
		{
			return new SchedulerService(_schedulerFactory, bus);
		}

		/// <summary>
		/// Returns the type of the service created by the configurator
		/// </summary>
		public Type ServiceType { get { return typeof (SchedulerService); } }
	}
}
