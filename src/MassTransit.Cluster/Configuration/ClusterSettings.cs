using System;

namespace MassTransit.Cluster.Configuration
{
	class ClusterSettings
	{
		public ClusterSettings()
		{
			ElectionPeriod = TimeSpan.FromSeconds(10);
			HeartbeatInterval = TimeSpan.FromSeconds(15);
		}

		/// <summary>
		/// The index of the current system in the grid
		/// </summary>
		public uint EndpointIndex { get; set; }

		/// <summary>
		/// The length of time to leave an election open, before declaring a winner
		/// </summary>
		public TimeSpan ElectionPeriod { get; set; }

		/// <summary>
		/// The length of time between heartbeats
		/// </summary>
		public TimeSpan HeartbeatInterval { get; set; }

		public event Action<IServiceBus> WonCoordinator;
		public void OnWonCoordinator(IServiceBus bus)
		{
			Action<IServiceBus> handler = WonCoordinator;
			if (handler != null) handler(bus);
		}

		public event Action LostCoordinator;
		public void OnLostCoordinator()
		{
			Action handler = LostCoordinator;
			if (handler != null) handler();
		}
	}
}
