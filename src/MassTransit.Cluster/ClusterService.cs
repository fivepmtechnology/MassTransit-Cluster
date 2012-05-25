using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.Cluster.Configuration;
using MassTransit.Util;

namespace MassTransit.Cluster
{
	class ClusterService : IBusService
	{
		private readonly ClusterSettings _settings;
		private readonly IList<uint> _clock;

		internal ClusterService([NotNull] ClusterSettings settings, IServiceBus bus)
		{
			_settings = settings;

			_clock = new List<uint>(_settings.SystemCount);
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			// no-op
		}

		/// <summary>
		/// Called when the service is being started, which is after the service bus has been started.
		/// </summary>
		/// <param name="bus">The service bus</param>
		public void Start(IServiceBus bus)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when the ServiceBus is being disposed, to allow any resources or subscriptions to be released.
		/// </summary>
		public void Stop()
		{
			throw new NotImplementedException();
		}
	}
}
