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

		public void SetEndpointIndex(uint index)
		{
			_settings.EndpointIndex = index;
		}

		public void SetEndpointCount(uint count)
		{
			_settings.EndpointCount = count;
		}

		public void SetElectionPeriod(TimeSpan period)
		{
			_settings.ElectionPeriod = period;
		}

		public void AddWonCoordinatorHandler(Action<IServiceBus> handler)
		{
			_settings.WonCoordinator += handler;
		}

		public void AddLostCoordinatorHandler(Action handler)
		{
			_settings.LostCoordinator += handler;
		}

		public void SetHeartbeatInterval(TimeSpan interval)
		{
			_settings.HeartbeatInterval = interval;
		}
	}
}
