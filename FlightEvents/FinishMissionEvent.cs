using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class FinishMissionEvent : FlightEvent
	{
		[JsonMember]
		public float finalMass;
		[JsonMember]
		public List<string> crewMembers;
	}
}
