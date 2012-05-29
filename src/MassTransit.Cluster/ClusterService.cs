using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MassTransit.Cluster.Configuration;
using MassTransit.Cluster.Messages;
using MassTransit.Logging;
using MassTransit.Util;

namespace MassTransit.Cluster
{
	class ClusterService : IBusService, Consumes<Election>.All, Consumes<Okay>.All, Consumes<Win>.All, Consumes<Heartbeat>.All
	{
		private readonly ClusterSettings _settings;
		private readonly IServiceBus _bus;
		private uint _coordinatorIndex;
		private readonly Timer _winnerTimer; // during an election -- wait for us to be the winner
		private readonly Timer _electionTimer; // during an idle period -- wait for no heartbeat, then run election
		private readonly ILog _log = Logger.Get(typeof(ClusterService));

		internal ClusterService([NotNull] ClusterSettings settings, IServiceBus bus)
		{
			_settings = settings;
			_bus = bus;

			_winnerTimer = new Timer(_ => Winner());
			_electionTimer = new Timer(_ => Election());
		}

		private void Election()
		{
			_log.Info("Holding a new election");

			_electionTimer.Change(Timeout.Infinite, Timeout.Infinite);

			// run an election
			// send an election notice to all systems with a higher id
			var message = new Election {SourceIndex = _settings.EndpointIndex};
			_bus.Publish(message);

			// set a timer; if no one responds with "okay" before it elapses, we're the winner
			_winnerTimer.Change(_settings.ElectionPeriod, TimeSpan.FromMilliseconds(-1));
		}

		private void Winner()
		{
			_log.Info("Won the election");

			// we won -- tell everyone!
			var message = new Win {SourceIndex = _settings.EndpointIndex};
			_bus.Publish(message);

			lock(_settings) _settings.OnWonCoordinator(_bus);
		}

		/// <summary>
		/// Whether or not this endpoint is the coordinator for the cluster
		/// </summary>
		public bool IsCoordinator { get { return _coordinatorIndex == _settings.EndpointIndex; } }

		public void Consume(Election message)
		{
			if (message.SourceIndex >= _settings.EndpointIndex)
				return;

			// respond to election message with okay
			var response = new Okay {SourceIndex = _settings.EndpointIndex};
			_bus.Publish(response);
		}

		public void Consume(Okay message)
		{
			if (message.SourceIndex <= _settings.EndpointIndex)
				return;

			// disable the timer, we're not the winner
			_winnerTimer.Change(Timeout.Infinite, Timeout.Infinite);

			// insist on heartbeats by waiting for 2*heartbeat rate for a heartbeat and holding an election otherwise
			var heartbeatWaitRate = new TimeSpan(_settings.HeartbeatInterval.Ticks*2);
			_electionTimer.Change(heartbeatWaitRate, TimeSpan.FromMilliseconds(-1));
		}

		public void Consume(Win message)
		{
			if(message.SourceIndex > _settings.EndpointIndex)
			{
				// someone higher has claimed coordinator
				_coordinatorIndex = message.SourceIndex;

				lock (_settings) _settings.OnLostCoordinator();
			}
			else if (message.SourceIndex < _settings.EndpointIndex)
			{
				// this shouldn't happen since we're alive! -- so let's hold a new election
				Election();
			}
		}

		void IDisposable.Dispose()
		{
			// no-op
		}

		/// <summary>
		/// Called when the service is being started, which is after the service bus has been started.
		/// </summary>
		/// <param name="bus">The service bus</param>
		public void Start(IServiceBus bus)
		{
			Election();
		}

		/// <summary>
		/// Called when the ServiceBus is being disposed, to allow any resources or subscriptions to be released.
		/// </summary>
		public void Stop()
		{
			
		}

		public void Consume(Heartbeat message)
		{
			if (message.SourceIndex != _coordinatorIndex)
				return;

			// but insist on heartbeats by waiting for 2*heartbeat rate for a heartbeat and holding an election otherwise
			var heartbeatWaitRate = new TimeSpan(_settings.HeartbeatInterval.Ticks * 2);
			_electionTimer.Change(heartbeatWaitRate, TimeSpan.FromMilliseconds(-1));
		}
	}
}
