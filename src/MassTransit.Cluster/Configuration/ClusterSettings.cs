namespace MassTransit.Cluster.Configuration
{
	class ClusterSettings
	{
		/// <summary>
		/// The index of the current system in the grid
		/// </summary>
		public uint EndpointIndex { get; set; }

		/// <summary>
		/// The count of total number of systems initially in the grid
		/// </summary>
		public uint EndpointCount { get; set; }
	}
}
