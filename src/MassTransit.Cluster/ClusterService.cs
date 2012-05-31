using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Magnum.StateMachine;
using MassTransit.Cluster.Configuration;
using MassTransit.Cluster.Messages;
using MassTransit.Logging;
using MassTransit.Util;

namespace MassTransit.Cluster
{
	public class ClusterService : StateMachine<ClusterService>, IBusService, Consumes<Election>.All, Consumes<Answer>.All, Consumes<Leader>.All, Consumes<Heartbeat>.All
	{
		private readonly ClusterSettings _settings;

		internal ClusterSettings Settings
		{
			get { return _settings; }
		}

		private readonly IServiceBus _bus;
		public uint? LeaderIndex { get; private set; }
		private readonly Timer _timer;
		private readonly Timer _heartbeatTimer; // send heartbeats periodically while the service is running
		private readonly ILog _log = Logger.Get(typeof(ClusterService));

		internal ClusterService([NotNull] ClusterSettings settings, [NotNull] IServiceBus bus)
		{
			_settings = settings;

			_bus = bus.ControlBus;

			_timer = new Timer(_ => RaiseEvent(TimerElapsed));
			_heartbeatTimer = new Timer(_ => RaiseEvent(SendHeartbeat));
		}

		#region Message Handlers

		public void Consume(Election message)
		{
			if (message.SourceIndex >= _settings.EndpointIndex)
				return;

			_log.InfoFormat("#{1} sees election from #{0}", message.SourceIndex, _settings.EndpointIndex);

			RaiseEvent(ElectionReceived);
		}

		public void Consume(Answer message)
		{
			if (message.SourceIndex <= _settings.EndpointIndex)
				return;

			_log.InfoFormat("#{1} sees answer from #{0}", message.SourceIndex, _settings.EndpointIndex);

			RaiseEvent(AnswerReceived);
		}

		public void Consume(Leader message)
		{
			if(message.SourceIndex < _settings.EndpointIndex)
				RaiseEvent(BadLeaderAnnounced);
			else if(message.SourceIndex > _settings.EndpointIndex)
				RaiseEvent(LeaderAnnounced, message);
		}

		public void Consume(Heartbeat message)
		{
			RaiseEvent(HeartbeatReceived);
		}

		#endregion

		void IDisposable.Dispose()
		{
			Stop();
		}

		#region IServiceBus Implementation

		/// <summary>
		/// Called when the service is being started, which is after the service bus has been started.
		/// </summary>
		/// <param name="bus">The service bus</param>
		public void Start(IServiceBus bus)
		{
			bus.SubscribeInstance(this);

			_heartbeatTimer.Change(_settings.HeartbeatInterval);

			RaiseEvent(ElectionReceived);
		}

		/// <summary>
		/// Called when the ServiceBus is being disposed, to allow any resources or subscriptions to be released.
		/// </summary>
		public void Stop()
		{
			RaiseEvent(StopRequested);
		}

		#endregion

		public static State Initial { get; set; } // when the endpoint first starts up
		public static State Idle { get; set; } // when the endpoint is not the leader
		public static State Leader { get; set; } // when the endpoint is the leader
		public static State Election { get; set; } // immediately after an election is called
		public static State WaitingForLeader { get; set; } // when our endpoint lost, but don't know who has won
		public static State Completed { get; set; } // when the bus stops the service

		public static Event ElectionReceived { get; set; }
		public static Event AnswerReceived { get; set; }
		public static Event SendHeartbeat { get; set; }
		public static Event HeartbeatReceived { get; set; }
		public static Event<Leader> LeaderAnnounced { get; set; }
		public static Event BadLeaderAnnounced { get; set; }
		public static Event TimerElapsed { get; set; }
		public static Event StopRequested { get; set; }

		private void HoldElection(bool initial = false)
		{
			// "P broadcasts an election message (inquiry) to all other processes with higher process IDs."

			_log.InfoFormat("#{0} is holding {1} election", _settings.EndpointIndex, initial ? "its initial" : "a new");

			// send an election notice to all systems with a higher id
			var message = new Election { SourceIndex = _settings.EndpointIndex };
			_bus.Publish(message);

			// set a timer; if no one responds with "okay" before it elapses, we're the winner
			_timer.Change(_settings.ElectionPeriod, null);

			// clear current leader
			LeaderIndex = null;
		}

		private void DeclareWinner()
		{
			// "If P hears from no process with a higher process ID than it, it wins the election and broadcasts victory."

			_log.InfoFormat("#{0} won the election", _settings.EndpointIndex);

			LeaderIndex = _settings.EndpointIndex; // win the election

			var message = new Leader { SourceIndex = _settings.EndpointIndex };
			_bus.Publish(message); // broadcast victory

			_settings.OnWonCoordinator(_bus);
		}

		static ClusterService()
		{
			Define(() =>
			{
				SetInitialState(Initial);
				SetCompletedState(Completed);

				Initially(
					When(ElectionReceived)
					.Then(workflow =>
					{
						workflow.HoldElection(true);
					})
					.TransitionTo(Election),

					When(StopRequested)
						.Complete()
				);
				
				During(Election,
					When(ElectionReceived)
					.Then(workflow =>
					{
						var response = new Answer {SourceIndex = workflow.Settings.EndpointIndex};
						workflow._bus.Publish(response);
					}),

					When(AnswerReceived)
					.Then(workflow =>
					{
						// "If P hears from a process with a higher ID, P waits a certain amount of time for that process to broadcast itself as the leader."
						// If it does not receive this message in time, it re-broadcasts the election message."

						workflow._log.InfoFormat("#{0} got an answer and lost the election", workflow.Settings.EndpointIndex);

						// wait for a leader announcement
						var doublePeriod = new TimeSpan(workflow.Settings.ElectionPeriod.Ticks*2);
						workflow._timer.Change(doublePeriod, null);
					})
					.TransitionTo(WaitingForLeader),

					When(LeaderAnnounced)
					.Then((workflow, message) =>
					{
						// "If P gets an election message (inquiry) from another process with a lower ID it sends an 'I am alive' message back and starts new elections."
						workflow._log.InfoFormat("#{1} sees #{0} won the election", message.SourceIndex, workflow.Settings.EndpointIndex);
						workflow.LeaderIndex = message.SourceIndex;
					})
					.TransitionTo(Idle),

					When(BadLeaderAnnounced)
					.Then(workflow =>
					{
						workflow._log.InfoFormat("#{0} sees an incorrect claim to have won", workflow.Settings.EndpointIndex);

						// "Note that if P receives a victory message from a process with a lower ID number, it immediately initiates a new election."
						workflow.HoldElection();
					})
					.TransitionTo(Election),

					When(TimerElapsed)
					.Then(workflow =>
					{
						workflow.DeclareWinner();
					})
					.TransitionTo(Leader),

					When(StopRequested)
						.Complete()
				);

				During(WaitingForLeader,
					When(LeaderAnnounced)
					.Then((workflow, message) =>
					{
						workflow._log.InfoFormat("#{1} sees #{0} won the election", message.SourceIndex, workflow.Settings.EndpointIndex);

						// save the announced leader index
						workflow.LeaderIndex = message.SourceIndex;
					})
					.TransitionTo(Idle),

					When(TimerElapsed)
					.Then(workflow =>
					{
						workflow.HoldElection();
					})
					.TransitionTo(Election),

					When(BadLeaderAnnounced)
					.Then(workflow =>
					{
						workflow._log.InfoFormat("#{0} sees an incorrect claim to have won", workflow.Settings.EndpointIndex);

						workflow.HoldElection();
					})
					.TransitionTo(Election),

					When(StopRequested)
						.Complete()
				);

				During(Leader,
					When(SendHeartbeat)
					.Then(workflow =>
					{
						workflow._log.DebugFormat("#{0} sends a heartbeat", workflow.Settings.EndpointIndex);
						var message = new Leader {SourceIndex = workflow.Settings.EndpointIndex};
						workflow._bus.Publish(message);
					}),

					When(ElectionReceived)
					.Then(workflow =>
					{
						var response = new Answer { SourceIndex = workflow.Settings.EndpointIndex };
						workflow._bus.Publish(response); // sends "I am alive" reply

						workflow.HoldElection();
					})
					.TransitionTo(Election),

					When(StopRequested)
						.Complete()
				);

				During(Idle,
					When(ElectionReceived)
					.Then(workflow =>
					{
						var response = new Answer { SourceIndex = workflow.Settings.EndpointIndex };
						workflow._bus.Publish(response); // sends "I am alive" reply
						
						workflow.HoldElection();
					})
					.TransitionTo(Election),

					When(TimerElapsed) // no heartbeat
					.Then(workflow =>
					{
						workflow.HoldElection();
					})
					.TransitionTo(Election),

					When(HeartbeatReceived)
					.Then(workflow =>
					{
						var doubleInterval = new TimeSpan(workflow.Settings.HeartbeatInterval.Ticks*2);
						workflow._timer.Change(doubleInterval);
					}),

					When(StopRequested)
						.Complete()
				);
			});
		}
	}
}
