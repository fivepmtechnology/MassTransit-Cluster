using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MassTransit.Cluster.Messages;

namespace MassTransit.Cluster.Subscriptions
{
	public class MasterElectionHandler : Consumes<IMessageContext<WantsMaster>>.All
	{
		public void Consume(IMessageContext<WantsMaster> messageContext)
		{

		}
	}
}
