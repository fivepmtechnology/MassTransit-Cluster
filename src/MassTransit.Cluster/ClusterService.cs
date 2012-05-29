using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.Cluster.Configuration;
using MassTransit.Cluster.Messages;
using MassTransit.Util;

namespace MassTransit.Cluster
{
	class ClusterService : IBusService, Consumes<Election>.Selected, Consumes<Okay>.Selected, Consumes<Win>.Selected
	{
		private readonly ClusterSettings _settings;
		private readonly IServiceBus _bus;
		private uint _master;

		internal ClusterService([NotNull] ClusterSettings settings, IServiceBus bus)
		{
			_settings = settings;
			_bus = bus;
		}

		private void Election()
		{
			// run an election
			// send an election notice to all systems with a higher id
			var message = new Election {SourceIndex = _settings.EndpointIndex};
			_bus.Publish(message);
		}

		public void Consume(Election message)
		{
			// respond to election message with okay
			var response = new Okay {SourceIndex = _settings.EndpointIndex};
			_bus.Publish(response);
		}
		
		public bool Accept(Election message)
		{
			// only allow lower endpoints to request an election
			return message.SourceIndex < _settings.EndpointIndex;
		}

		public void Consume(Okay message)
		{
			_master = Math.Max(message.SourceIndex, _master);
		}

		public bool Accept(Okay message)
		{
			// only listen to higher endpoints telling us they're okay
			return message.SourceIndex > _settings.EndpointIndex;
		}

		public void Consume(Win message)
		{
			_master = Math.Max(message.SourceIndex, _master);
		}

		public bool Accept(Win message)
		{
			// only allow higher endpoints to "win" the leader
			return message.SourceIndex > _settings.EndpointIndex;
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
