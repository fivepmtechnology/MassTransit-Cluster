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

		public event Action<IServiceBus> Promotion;
		public void OnPromotion(IServiceBus bus)
		{
			Action<IServiceBus> handler = Promotion;
			if (handler != null) handler(bus);
		}

		public event Action Demotion;
		public void OnDemotion()
		{
			Action handler = Demotion;
			if (handler != null) handler();
		}
	}
}
