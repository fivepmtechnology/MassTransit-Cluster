using System;

namespace MassTransit.Cluster.Configuration
{
	class ClusterSettings
	{
		public ClusterSettings()
		{
			ElectionPeriod = TimeSpan.FromSeconds(30);
			HeartbeatInterval = TimeSpan.FromSeconds(15);
		}

		/// <summary>
		/// The index of the current system in the grid
		/// </summary>
		public uint EndpointIndex { get; set; }

		/// <summary>
		/// The count of total number of systems initially in the grid
		/// </summary>
		public uint EndpointCount { get; set; }

		/// <summary>
		/// The length of time to leave an election open, before declaring a winner
		/// </summary>
		public TimeSpan ElectionPeriod { get; set; }

		/// <summary>
		/// The length of time between heartbeats
		/// </summary>
		public TimeSpan HeartbeatInterval { get; set; }
	}
}
