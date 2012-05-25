using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.BusServiceConfigurators;

namespace MassTransit.Cluster.Configuration
{
	class ClusterConfigurator : BusServiceConfigurator, IClusterConfigurator
	{
		/// <summary>
		/// Creates the service
		/// </summary>
		/// <param name="bus"/>
		/// <returns>
		/// The instance of the service
		/// </returns>
		public IBusService Create(IServiceBus bus)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns the type of the service created by the configurator
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
