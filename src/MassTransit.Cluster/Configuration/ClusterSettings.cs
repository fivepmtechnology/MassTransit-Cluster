namespace MassTransit.Cluster
{
	public class ClusterSettings
	{
		/// <summary>
		/// The index of the current system in the grid
		/// </summary>
		public int SystemIndex { get; set; }

		/// <summary>
		/// The count of total number of systems initially in the grid
		/// </summary>
		public int SystemCount { get; set; }
	}
}
