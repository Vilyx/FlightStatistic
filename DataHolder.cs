using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class DataHolder
	{
		[JsonMember]
		public List<LaunchEvent> launches;
		[JsonMember]
		public List<LaunchCrewEvent> crewLaunches;
		[JsonMember]
		public bool useNativeGui = true;
	}
}
