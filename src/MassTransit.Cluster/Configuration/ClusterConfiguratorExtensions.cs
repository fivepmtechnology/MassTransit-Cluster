using System;
using MassTransit.BusConfigurators;
using MassTransit.BusServiceConfigurators;

namespace MassTransit.Cluster.Configuration
{
    public static class ClusterConfiguratorExtensions
    {
		public static T UseClusterService<T>(this T configurator, Action<IClusterConfigurator> configure)
			where T : ServiceBusConfigurator
		{
			configurator.UseControlBus();

			var cfg = new ClusterConfigurator();
			configure(cfg);

			configurator.AddBusConfigurator(new CustomBusServiceConfigurator(cfg));

			return configurator;
		}
    }
}