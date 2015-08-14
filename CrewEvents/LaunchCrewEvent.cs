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
		public double maxGee;
		[JsonMember]
		public string vesselName;

		public override bool Revert(long currentTime)
		{
			bool removed = base.Revert(currentTime);
			maxGee = GetEventMaxGee();
			return removed;
		}
		internal double GetEventMaxGee()
		{
			double gee = 0;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is MaxGeeCrewEvent)
				{
					gee = (flightEvent as MaxGeeCrewEvent).gee;
				}
			}
			return gee;
		}
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
		private string TicksToShortTime(object p)
		{
			if (p == null) return "-";
			long ticks = long.Parse(p.ToString());
			long days = ticks / (TimeSpan.TicksPerDay / 4);
			long hours = ticks % (TimeSpan.TicksPerDay / 4) / TimeSpan.TicksPerHour;
			long minutes = ticks % TimeSpan.TicksPerHour / TimeSpan.TicksPerMinute;
			if(hours < 10 && days < 1)
				return days.ToString() + "d " + hours.ToString() + "h " + minutes + "m";
			return days.ToString() + "d " + hours.ToString() + "h";
		}

		internal IEnumerable<string> GetShips()
		{
			List<string> ships = new List<string>();
			//ships.Add(vesselName);
			LaunchCrewEvent lauEv = this;
			List<EvaCrewEvent> evas = new List<EvaCrewEvent>();
			foreach (var flightEvent in subsequentEvents)
			{
				if (flightEvent is LaunchCrewEvent)
				{
					evas = new List<EvaCrewEvent>();
					lauEv = flightEvent as LaunchCrewEvent;
				}
				else if (flightEvent is EvaCrewEvent)
				{
					evas.Add(flightEvent as EvaCrewEvent);
				}
				else if (flightEvent is EndFlightCrewEvent)
				{
					ships.Add(lauEv.vesselName + "(" + TicksToShortTime(flightEvent.time - lauEv.time) + ", EVA - " + evas.Count.ToString() + ")");
					lauEv = null;
					evas = new List<EvaCrewEvent>();
				}
			}

			if (lauEv != null)
			{
				ships.Add(lauEv.vesselName + "(" + TicksToShortTime(EventProcessor.GetTimeInTicks() - lauEv.time) + ", EVA - " + evas.Count.ToString() + ")");
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
