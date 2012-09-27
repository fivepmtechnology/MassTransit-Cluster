using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Magnum.StateMachine;
using MassTransit.Cluster.Bully.Messages;
using MassTransit.Cluster.Configuration;
using MassTransit.Cluster.Messages;
using MassTransit.Logging;

namespace MassTransit.Cluster.Bully
{
    public class BullyHandler : StateMachine<BullyHandler>, Consumes<Election>.All, Consumes<Answer>.All, Consumes<Leader>.All
    {
        private readonly ClusterService _clusterService;
        private ClusterSettings _settings;

        private readonly ILog _log = Logger.Get(typeof(BullyHandler));

        public uint? LeaderIndex { get; private set; }
    	private readonly ISet<IClusterService> _services = new HashSet<IClusterService>();
        private Timer _timer;

        public uint ComputeIndex(Uri address)
        {
            return (uint) (address.GetHashCode() + (long) int.MinValue);
        }
        
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

            RaiseEvent(AnswerReceived);
        }

        public void Consume(Leader message)
        {
            if (message.SourceIndex < _settings.EndpointIndex)
                RaiseEvent(BadLeaderAnnounced);
            else if (message.SourceIndex > _settings.EndpointIndex)
                RaiseEvent(LeaderAnnounced, message.SourceIndex);
        }

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
        public static Event<uint> LeaderAnnounced { get; set; }
        public static Event BadLeaderAnnounced { get; set; }
        public static Event TimerElapsed { get; set; }
        public static Event StopRequested { get; set; }

        private void HoldElection(bool initial = false)
        {
            // "P broadcasts an election message (inquiry) to all other processes with higher process IDs."

            _log.InfoFormat("#{0} is holding {1} election", _settings.EndpointIndex, initial ? "its initial" : "a new");

            // send an election notice to all systems with a higher id
            var message = new Election { SourceIndex = _settings.EndpointIndex };
            _clusterService.Bus.Publish(message);

            // set a timer; if no one responds with "okay" before it elapses, we're the winner
            _timer.Change(_settings.Timeout, null);

            // clear current leader
            LeaderIndex = null;
        }

		private void DeclareWinner()
		{
			// "If P hears from no process with a higher process ID than it, it wins the election and broadcasts victory."

			_log.InfoFormat("#{0} won the election", _settings.EndpointIndex);

			LeaderIndex = _settings.EndpointIndex; // win the election

			var message = new Leader {SourceIndex = _settings.EndpointIndex};
			_clusterService.Bus.Publish(message); // broadcast victory

            // take winner actions
		}

    	static BullyHandler()
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
                        var response = new Answer { SourceIndex = workflow._settings.EndpointIndex };
                        workflow._clusterService.Bus.Publish(response);
                    }),

                    When(AnswerReceived)
                    .Then(workflow =>
                    {
                        // "If P hears from a process with a higher ID, P waits a certain amount of time for that process to broadcast itself as the leader."
                        // If it does not receive this message in time, it re-broadcasts the election message."

                        workflow._log.InfoFormat("#{0} got an answer and lost the election", workflow._settings.EndpointIndex);

                        // wait for a leader announcement
                        var doublePeriod = new TimeSpan(workflow._settings.Timeout.Ticks * 2);
                        workflow._timer.Change(doublePeriod, null);
                    })
                    .TransitionTo(WaitingForLeader),

                    When(LeaderAnnounced)
                    .Then((workflow, index) =>
                    {
                        // "If P gets an election message (inquiry) from another process with a lower ID it sends an 'I am alive' message back and starts new elections."
                        workflow._log.InfoFormat("#{1} sees #{0} won the election", index, workflow._settings.EndpointIndex);
                        workflow.LeaderIndex = index;

                        var doubleInterval = new TimeSpan(workflow._settings.HeartbeatInterval.Ticks * 2);
                        workflow._timer.Change(doubleInterval);
                    })
                    .TransitionTo(Idle),

                    When(BadLeaderAnnounced)
                    .Then(workflow =>
                    {
                        workflow._log.InfoFormat("#{0} sees an incorrect claim to have won", workflow._settings.EndpointIndex);

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
                    .Then((workflow, index) =>
                    {
                        workflow._log.InfoFormat("#{1} sees #{0} won the election", index, workflow._settings.EndpointIndex);

                        // save the announced leader index
                        workflow.LeaderIndex = index;

						// wait for heartbeats
						var doubleInterval = new TimeSpan(workflow._settings.HeartbeatInterval.Ticks * 2);
						workflow._timer.Change(doubleInterval);
                    })
                    .TransitionTo(Idle),

                    When(TimerElapsed)
                    .Then(workflow =>
                    {
                        workflow._log.InfoFormat("#{0} got no leader", workflow._settings.EndpointIndex);

                        workflow.HoldElection();
                    })
                    .TransitionTo(Election),

                    When(BadLeaderAnnounced)
                    .Then(workflow =>
                    {
                        workflow._log.InfoFormat("#{0} sees an incorrect claim to have won", workflow._settings.EndpointIndex);

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
                        workflow._log.DebugFormat("#{0} sends a heartbeat", workflow._settings.EndpointIndex);
                        var message = new Heartbeat { SourceIndex = workflow._settings.EndpointIndex };
                        workflow._clusterService.Bus.Publish(message);
                    }),

                    When(ElectionReceived)
                    .Then(workflow =>
                    {
                        workflow._log.InfoFormat("#{0} got an election request", workflow._settings.EndpointIndex);

                        var response = new Answer { SourceIndex = workflow._settings.EndpointIndex };
                        workflow._clusterService.Bus.Publish(response); // sends "I am alive" reply

                        // todo: demotion action

                    	workflow.HoldElection();
                    })
                    .TransitionTo(Election),

                    When(BadLeaderAnnounced)
                    .Then(workflow =>
                    {
                        workflow._log.InfoFormat("#{0} sees an incorrect claim to have won", workflow._settings.EndpointIndex);

                        // todo: demotion action

                        workflow.HoldElection();
                    }),

					When(LeaderAnnounced)
					.Then((workflow, index) =>
					{
						workflow._log.InfoFormat("#{1} sees #{0} won the election", index, workflow._settings.EndpointIndex);

						// save the announced leader index
						workflow.LeaderIndex = index;

                        // todo: demotion action

						// wait for heartbeats
						var doubleInterval = new TimeSpan(workflow._settings.HeartbeatInterval.Ticks * 2);
						workflow._timer.Change(doubleInterval);
					})
					.TransitionTo(Idle),

                    When(StopRequested)
                        .Complete()
                );

                During(Idle,
                    When(ElectionReceived)
                    .Then(workflow =>
                    {
                        var response = new Answer { SourceIndex = workflow._settings.EndpointIndex };
                        workflow._clusterService.Bus.Publish(response); // sends "I am alive" reply

                        workflow.HoldElection();
                    })
                    .TransitionTo(Election),

                    When(TimerElapsed) // no heartbeat
                    .Then(workflow =>
                    {
                        workflow._log.WarnFormat("#{0} did not get leader heartbeat", workflow._settings.EndpointIndex);

                        workflow.HoldElection();
                    })
                    .TransitionTo(Election),

                    When(HeartbeatReceived)
                    .Then(workflow =>
                    {
						workflow._log.DebugFormat("#{0} sees a heartbeat", workflow._settings.EndpointIndex);

                        var doubleInterval = new TimeSpan(workflow._settings.HeartbeatInterval.Ticks * 2);
                        workflow._timer.Change(doubleInterval);
                    }),

                    When(BadLeaderAnnounced)
                    .Then(workflow =>
                    {
                        workflow._log.InfoFormat("#{0} sees an incorrect claim to have won", workflow._settings.EndpointIndex);

                        workflow.HoldElection();
                    }),

                    When(StopRequested)
                        .Complete()
                );
            });
        }

        public BullyHandler(ClusterService clusterService)
        {
            _clusterService = clusterService;
        }
    }
}
