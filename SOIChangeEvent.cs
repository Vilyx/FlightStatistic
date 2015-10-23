using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class SOIChangeEvent : FlightEvent
	{
		[JsonMember]
		public string soiName;
		[JsonMember]
		public float mass;
	}
}
