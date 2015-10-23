using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class ScienceEvent : FlightEvent
	{
		[JsonMember]
		public float sciencePoints;
		[JsonMember]
		public string title;
	}
}
