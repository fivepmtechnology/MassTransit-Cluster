using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MassTransit.Cluster
{
	public static class TimerExtensions
	{
		public static TimeSpan Infinite { get { return TimeSpan.FromMilliseconds(-1); } }

		public static bool Change(this Timer timer, TimeSpan? dueTime, TimeSpan? interval)
		{
			return timer.Change(dueTime ?? Infinite, interval ?? Infinite);
		}

		public static bool Change(this Timer timer, TimeSpan? interval)
		{
			return timer.Change(interval ?? Infinite, interval ?? Infinite);
		}

		public static bool Stop(this Timer timer)
		{
			return timer.Change(Infinite, Infinite);
		}
	}
}
