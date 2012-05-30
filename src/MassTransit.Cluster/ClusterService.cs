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
	public class ClusterService : IBusService, Consumes<Election>.All, Consumes<Answer>.All, Consumes<Leader>.All, Consumes<Heartbeat>.All
	{
		private readonly ClusterSettings _settings;
		private readonly IServiceBus _bus;
		private uint? _leaderIndex;
		private readonly Timer _winnerTimer; // during an election -- wait for us to be the winner
		private readonly Timer _electionTimer; // during an idle period -- wait for no heartbeat, then run election
		private readonly Timer _heartbeatTimer; // send heartbeats while we're the leader
		private readonly ILog _log = Logger.Get(typeof(ClusterService));

		private static readonly TimeSpan Infinite = TimeSpan.FromMilliseconds(-1);

		internal ClusterService([NotNull] ClusterSettings settings, IServiceBus bus)
		{
			_settings = settings;

			if (bus.ControlBus != null)
				_bus = bus.ControlBus;
			else
				_bus = bus;

			_winnerTimer = new Timer(_ => DeclareWinner());
			_electionTimer = new Timer(_ => HoldElection());
			_heartbeatTimer = new Timer(_ => SendHeartbeat());
		}

		private readonly Random _random = new Random();

		private void HoldElection(bool initial = false)
		{
			// "P broadcasts an election message (inquiry) to all other processes with higher process IDs."

			_log.InfoFormat("#{0} is holding {1} election", _settings.EndpointIndex, initial ? "its initial" : "a new");

			_electionTimer.Stop();

			// send an election notice to all systems with a higher id
			var message = new Election { SourceIndex = _settings.EndpointIndex };
			_bus.Publish(message);

			// set a timer; if no one responds with "okay" before it elapses, we're the winner
			_winnerTimer.Change(_settings.ElectionPeriod, null);

			// clear current leader
			_leaderIndex = null;
		}

		private void DeclareWinner()
		{
			// "If P hears from no process with a higher process ID than it, it wins the election and broadcasts victory."

			_log.InfoFormat("#{0} won the election", _settings.EndpointIndex);

			_leaderIndex = _settings.EndpointIndex; // win the election

			var message = new Leader { SourceIndex = _settings.EndpointIndex };
			_bus.Publish(message); // broadcast victory

			// start heartbeating
			_heartbeatTimer.Change(TimeSpan.Zero, _settings.HeartbeatInterval);

			lock (_settings) _settings.OnWonCoordinator(_bus);
		}

		private void DeclareLoser()
		{
			// "If P hears from a process with a higher ID, P waits a certain amount of time for that process to broadcast itself as the leader.
			// If it does not receive this message in time, it re-broadcasts the election message."

			_log.InfoFormat("#{0} lost the election", _settings.EndpointIndex);

			// we lost, so we can't win anymore
			_winnerTimer.Stop();

			// insist on heartbeats by waiting for 2*heartbeat rate for a heartbeat, holding an election if no reply
			var heartbeatWaitRate = new TimeSpan(_settings.HeartbeatInterval.Ticks * 2) + TimeSpan.FromSeconds(_random.NextDouble() * 4.0 - 2.0);
			_electionTimer.Change(heartbeatWaitRate, Infinite);
		}

		private void SendHeartbeat()
		{
            _log.DebugFormat("#{0} sends a heartbeat", _settings.EndpointIndex);
			var message = new Leader {SourceIndex = _settings.EndpointIndex};
			_bus.Publish(message);
		}

		/// <summary>
		/// Whether or not this endpoint is the coordinator for the cluster
		/// </summary>
		public bool IsLeader { get { return _leaderIndex == _settings.EndpointIndex; } }

		public void Consume(Election message)
		{
			// "If P gets an election message (inquiry) from another process with a lower ID it sends an 'I am alive' message back and starts new elections."

			if (message.SourceIndex >= _settings.EndpointIndex)
				return;

			_log.InfoFormat("#{1} sees election requested by endpoint #{0}", message.SourceIndex, _settings.EndpointIndex);

			var response = new Answer { SourceIndex = _settings.EndpointIndex };
			_bus.Publish(response); // sends "I am alive"

			HoldElection(); // starts new elections
		}

		public void Consume(Answer message)
		{
			if (message.SourceIndex <= _settings.EndpointIndex)
				return;

			if (_leaderIndex != null)
				return; // we're not waiting for a response

			DeclareLoser();
		}

		public void Consume(Leader message)
		{
			if (message.SourceIndex > _settings.EndpointIndex)
			{
				_log.InfoFormat("#{1} sees #{0} won the election", message.SourceIndex, _settings.EndpointIndex);

				_leaderIndex = message.SourceIndex;

				DeclareLoser();

				lock (_settings) _settings.OnLostCoordinator();
			}
			else if (message.SourceIndex < _settings.EndpointIndex)
			{
				// "Note that if P receives a victory message from a process with a lower ID number, it immediately initiates a new election."
				HoldElection();
			}
		}

		void IDisposable.Dispose()
		{
			Stop();
		}

		/// <summary>
		/// Called when the service is being started, which is after the service bus has been started.
		/// </summary>
		/// <param name="bus">The service bus</param>
		public void Start(IServiceBus bus)
		{
			bus.SubscribeInstance(this);

			HoldElection(true);
		}

		/// <summary>
		/// Called when the ServiceBus is being disposed, to allow any resources or subscriptions to be released.
		/// </summary>
		public void Stop()
		{
			_electionTimer.Stop();
			_winnerTimer.Stop();
			_heartbeatTimer.Stop();
		}

		public void Consume(Heartbeat message)
		{
			throw new NotImplementedException();
		}
	}
}
