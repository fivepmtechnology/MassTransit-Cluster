using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassTransit.Services.Timeout.Messages;
using Quartz;
using Quartz.Impl;

namespace MassTransit.Cluster.Scheduler
{
	public class TimeoutHandlers : Consumes<ScheduleTimeout>.All, Consumes<CancelTimeout>.All
	{
		private const string CorrelationIdKey = "CorrelationId";
		private const string TagKey = "Tag";

		private class TimeoutMessageJob : IJob
		{
			private readonly IServiceBus _bus;

			public void Execute(IJobExecutionContext context)
			{
				var correlationId = (Guid)context.MergedJobDataMap[CorrelationIdKey];
				var tag = (int)context.MergedJobDataMap[TagKey];
				var message = new TimeoutExpired
				{
					CorrelationId = correlationId,
					Tag = tag
				};
				_bus.Publish(message);
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

		public void Consume(ScheduleTimeout message)
		{
			// construct job info
			var jobBuilder = JobBuilder.Create<TimeoutMessageJob>();
			jobBuilder.UsingJobData(new JobDataMap
			{
				{CorrelationIdKey, message.CorrelationId},
				{TagKey, message.Tag}
			});
			jobBuilder.WithIdentity(CorrelationIdToJobKey(message.CorrelationId));
			var jobDetail = jobBuilder.Build();

			var triggerBuilder = TriggerBuilder.Create();
			triggerBuilder.StartAt(new DateTimeOffset(message.TimeoutAt));
			var trigger = triggerBuilder.Build();
			// start on the next even hour
			_scheduler.ScheduleJob(jobDetail, trigger);
		}

		public void Consume(CancelTimeout message)
		{
			var jobKey = CorrelationIdToJobKey(message.CorrelationId);
			_scheduler.DeleteJob(jobKey);
		}
	}
}
