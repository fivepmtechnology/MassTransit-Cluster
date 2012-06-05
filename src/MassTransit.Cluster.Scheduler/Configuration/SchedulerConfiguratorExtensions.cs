using System;
using MassTransit.BusConfigurators;
using MassTransit.BusServiceConfigurators;
using MassTransit.Cluster.Configuration;
using Quartz;

namespace MassTransit.Cluster.Scheduler.Configuration
{
    public static class SchedulerConfiguratorExtensions
    {
		public static T UseScheduler<T>(this T configurator, Func<IScheduler> schedulerFactory)
			where T : IClusterConfigurator
		{
			var cfg = new SchedulerConfigurator(schedulerFactory);

			configurator.AddClusterServiceConfigurator(cfg);

			return configurator;
		}
    }
}
