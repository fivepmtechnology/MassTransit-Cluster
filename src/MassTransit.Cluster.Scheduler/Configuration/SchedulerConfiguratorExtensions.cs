using MassTransit.BusConfigurators;
using Quartz;

namespace MassTransit.Cluster.Scheduler.Configuration
{
    public static class SchedulerConfiguratorExtensions
    {
		public static T UseQuartz<T>(this T configurator, IScheduler scheduler)
			where T : ServiceBusConfigurator
		{
			return configurator;
		}
    }
}
