using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class LaunchCrewEvent : FlightEvent
	{
		[JsonMember]
		public string name;
		public int posInTable;
		[JsonMember]
		public string vesselName;


		public int GetLaunchesCount()
		{
			int count = 1;
			foreach (var flightEvent in subsequentEvents)
			{
				if (flightEvent.GetType() == typeof(LaunchCrewEvent))
					count++;
			}
			return count;
		}

		public long GetTotalFlightTime()
		{
			long totalTime = 0;
			FlightEvent currentStart = this;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is LaunchCrewEvent)
				{
					currentStart = flightEvent;
				}
				else if (flightEvent is EndFlightCrewEvent)
				{
					totalTime += flightEvent.time - currentStart.time;
					currentStart = null;
				}
			}
			if(currentStart != null)
				totalTime += EventProcessor.GetTimeInTicks() - currentStart.time;

			return totalTime;
		}

		internal object GetTotalEvasTime()
		{
			long totalTime = 0;
			FlightEvent currentStart = null;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is EvaCrewEvent)
				{
					currentStart = flightEvent;
				}
				else if (currentStart != null && (flightEvent is EndFlightCrewEvent || flightEvent is EvaCrewEndEvent))
				{
					totalTime += flightEvent.time - currentStart.time;
					currentStart = null;
				}
			}
			if (currentStart != null)
				totalTime += EventProcessor.GetTimeInTicks() - currentStart.time;

			return totalTime;
		}

		internal object GetEvasCount()
		{
			int count = 0;
			foreach (var flightEvent in subsequentEvents)
			{
				if (flightEvent is EvaCrewEvent)
					count++;
			}
			return count;
		}

		internal object GetDockingsCount()
		{
			int count = 0;
			foreach (var flightEvent in subsequentEvents)
			{
				if (flightEvent is DockingCrewEvent)
					count++;
			}
			return count;
		}

		internal object GetLandingsCount()
		{
			int count = 0;
			foreach (var flightEvent in subsequentEvents)
			{
				if (flightEvent is LandingCrewEvent)
					count++;
			}
			return count;
		}

		internal IEnumerable<string> GetShips()
		{
			List<string> ships = new List<string>();
			ships.Add(vesselName);
			foreach (var flightEvent in subsequentEvents)
			{
				if (flightEvent is LaunchCrewEvent)
				{
					var lauEv = flightEvent as LaunchCrewEvent;
					ships.Add(lauEv.vesselName);
				}
			}
			return ships;
		}

		internal object GetIdleTime()
		{
			long totalTime = 0;
			FlightEvent currentEnd = null;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is EndFlightCrewEvent)
				{
					currentEnd = flightEvent;
				}
				else if (currentEnd != null && flightEvent is LaunchCrewEvent)
				{
					currentEnd = null;
				}
			}
			if (currentEnd != null)
				totalTime += EventProcessor.GetTimeInTicks() - currentEnd.time;

			return totalTime;
		}

		internal bool IsAlive()
		{
			foreach (var flightEvent in subsequentEvents)
			{
				if (flightEvent is DeathCrewEvent)
				{
					return false;
				}
			}
			return true;
		}
	}
}
