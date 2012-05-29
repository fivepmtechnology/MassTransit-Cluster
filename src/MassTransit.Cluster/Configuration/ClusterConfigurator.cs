using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.BusServiceConfigurators;

namespace MassTransit.Cluster.Configuration
{
	class ClusterConfigurator : BusServiceConfigurator, IClusterConfigurator
	{
		private readonly ClusterSettings _settings = new ClusterSettings();

		/// <summary>
		/// Creates the cluster service instance
		/// </summary>
		/// <param name="bus"/>
		/// <returns>
		/// The instance of the service
		/// </returns>
		public IBusService Create(IServiceBus bus)
		{
			var service = new ClusterService(_settings, bus);
			bus.SubscribeInstance(service);
			return service;
		}

		/// <summary>
		/// Returns the type of the service created by this configurator
		/// </summary>
		public Type ServiceType
		{
			get { return typeof (ClusterService); }
		}

		public BusServiceLayer Layer
		{
			get { return BusServiceLayer.Session; }
		}
	}
}
