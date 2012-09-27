using System;
using System.Collections.Generic;

namespace MassTransit.Cluster.Configuration
{
	class ClusterSettings
	{
		public ClusterSettings()
		{
		    // defaults
		    HeartbeatInterval = TimeSpan.FromSeconds(15);
		}

		/// <summary>
		/// The length of time between heartbeats. (Default: 15s)
		/// </summary>
		public TimeSpan HeartbeatInterval { get; set; }

        /// <summary>
        /// The total number of endpoints.
        /// </summary>
        public uint EndpointCount { get; set; }

        /// <summary>
        /// The numeric index of the endpoint
        /// </summary>
        public uint EndpointIndex { get; internal set; }

        /// <summary>
        /// The timeout before an endpoint is decided to be dead.
        /// </summary>
        public TimeSpan Timeout { get; set; }
	}
}
