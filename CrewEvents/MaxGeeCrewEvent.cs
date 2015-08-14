﻿using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class MaxGeeCrewEvent : FlightEvent
	{
		[JsonMember]
		public double gee;
	}
}
