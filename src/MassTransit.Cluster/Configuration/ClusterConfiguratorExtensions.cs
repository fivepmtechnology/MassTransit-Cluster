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
			var cfg = new ClusterConfigurator(configurator);
			configure(cfg);

			configurator.AddBusConfigurator(new CustomBusServiceConfigurator(cfg));

			return configurator;
		}
    }
}