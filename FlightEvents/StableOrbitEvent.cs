using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class StableOrbitEvent : FlightEvent
	{
		[JsonMember]
		public float mass;
	}
}
