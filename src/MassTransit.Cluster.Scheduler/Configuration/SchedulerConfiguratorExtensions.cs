using MassTransit.BusConfigurators;
using MassTransit.BusServiceConfigurators;
using MassTransit.Cluster.Configuration;
using Quartz;

namespace MassTransit.Cluster.Scheduler.Configuration
{
    public static class SchedulerConfiguratorExtensions
    {
		public static T UseScheduler<T>(this T configurator, IScheduler scheduler)
			where T : IClusterConfigurator
		{
			var cfg = new SchedulerConfigurator(scheduler);

			configurator.AddPromotionHandler(bus => scheduler.Start());
			configurator.AddDemotionHandler(scheduler.Standby);

			configurator.BusConfigurator.AddBusConfigurator(new CustomBusServiceConfigurator(cfg));
			return configurator;
		}
    }
}
