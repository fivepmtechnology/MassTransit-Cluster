using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MassTransit.Cluster
{
	public interface IClusterService
	{
		/// <summary>
		/// Called when the service bus endpoint is promoted to leader
		/// </summary>
		/// <param name="bus">the service bus</param>
		void Promoted(IServiceBus bus);

		/// <summary>
		/// Called when the service bus is demoted from leader
		/// </summary>
		void Demoted();

		void Start(IServiceBus bus);

		void Stop();
	}
}
