using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class LandingEvent : FlightEvent
	{
		[JsonMember]
		public string mainBodyName;
		[JsonMember]
		public string biome;
	}
}
