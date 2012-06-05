using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit.Logging;
using MassTransit.Services.Timeout.Messages;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace MassTransit.Cluster.Scheduler
{
	public class TimeoutHandlers : Consumes<ScheduleTimeout>.Context, Consumes<CancelTimeout>.All
	{
		private const string CorrelationIdKey = "CorrelationId";
		private const string TagKey = "Tag";
		private const string MessageIdKey = "MessageId";

		private readonly ILog _log = Logger.Get(typeof(TimeoutHandlers));

		private class TimeoutMessageJob : IJob
		{
			private readonly IServiceBus _bus;

			private readonly ILog _log = Logger.Get(typeof(TimeoutMessageJob));

			public TimeoutMessageJob(IServiceBus bus)
			{
				_bus = bus;
			}

			public void Execute(IJobExecutionContext context)
			{
				_log.Debug("Executing scheduled timeout job");

				try
				{
					var correlationId = (Guid) context.MergedJobDataMap[CorrelationIdKey];
					var tag = (int) context.MergedJobDataMap[TagKey];
					var message = new TimeoutExpired
					{
						CorrelationId = correlationId,
						Tag = tag
					};
					_bus.Publish(message);
				}
				catch(Exception ex)
				{
					throw new JobExecutionException(ex);
				}

				_log.Debug("Published timeout message");
			}
		}

		private readonly IScheduler _scheduler;

		public TimeoutHandlers(IScheduler scheduler)
		{
			_scheduler = scheduler;
		}

		JobKey CorrelationIdToJobKey(Guid correlationId)
		{
			return new JobKey(correlationId.ToString(), null);
		}

		public void Consume(IConsumeContext<ScheduleTimeout> message)
		{
			_log.Debug("Scheduling timeout with Quartz");
			
			// construct job info
			var jobBuilder = JobBuilder.Create<TimeoutMessageJob>();
			jobBuilder.UsingJobData(new JobDataMap
			{
				{CorrelationIdKey, message.Message.CorrelationId},
				{TagKey, message.Message.Tag},
				{MessageIdKey, message.MessageId}
			});
			jobBuilder.WithIdentity(CorrelationIdToJobKey(message.Message.CorrelationId));
			jobBuilder.RequestRecovery(true);
			jobBuilder.StoreDurably(false);
			var jobDetail = jobBuilder.Build();

			var triggerBuilder = TriggerBuilder.Create();
			triggerBuilder.StartAt(new DateTimeOffset(message.Message.TimeoutAt));
			var trigger = triggerBuilder.Build();
			_scheduler.ScheduleJob(jobDetail, trigger);
		}

		public void Consume(CancelTimeout message)
		{
			var jobKey = CorrelationIdToJobKey(message.CorrelationId);
			_scheduler.DeleteJob(jobKey);
		}
	}
}
