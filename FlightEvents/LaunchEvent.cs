using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	[JsonOptIn]
	class LaunchEvent : FlightEvent
	{
		public int posInTable;
		[JsonMember]
		public string rootPartID;
		[JsonMember]
		public string shipName;
		[JsonMember]
		public string shipID;
		[JsonMember]
		public List<StatisticVehiclePart> parts;
		[JsonMember]
		public float launchCost;
		[JsonMember]
		public float launchMass;
		[JsonMember]
		public List<string> crewMembers = new List<string>();
		[JsonMember]
		public double maxSpeed;
		[JsonMember]
		public float sciencePoints;
		[JsonMember]
		public double maxGee;
		[JsonMember]
		public float currentMass;

		public override bool Revert(long currentTime)
		{
			bool removed = base.Revert(currentTime);
			maxGee = GetEventMaxGee();
			maxSpeed = GetEventMaxSpeed();
			return removed;
		}
		public long GetTotalFlightTime()
		{
			long totalTime = 0;
			FlightEvent currentStart = this;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is LaunchEvent)
				{
					currentStart = flightEvent;
				}
				else if (flightEvent is EndFlightEvent)
				{
					totalTime += flightEvent.time - currentStart.time;
					currentStart = null;
				}
			}
			if (currentStart != null)
				totalTime += EventProcessor.GetTimeInTicks() - currentStart.time;

			return totalTime;
		}
		public long GetTotalMissionTime()
		{
			long totalTime = 0;
			FlightEvent currentStart = this;
			FlightEvent lastStart = null;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is LaunchEvent)
				{
					currentStart = flightEvent;
				}
				else if (flightEvent is EndFlightEvent || flightEvent is FinishMissionEvent)
				{
					if (currentStart == null) currentStart = lastStart;
					totalTime += flightEvent.time - currentStart.time;
					lastStart = currentStart;
					currentStart = null;
				}
			}
			if (currentStart != null)
				totalTime += EventProcessor.GetTimeInTicks() - currentStart.time;

			return totalTime;
		}

		public FlightEvent GetLastEvent()
		{
			if (subsequentEvents == null || subsequentEvents.Count == 0) return null;
			FlightEvent flightEvent = subsequentEvents[subsequentEvents.Count - 1];
			return flightEvent;
		}
		internal long GetEndDate()
		{
			if (subsequentEvents.Count == 0) return -1;
			FlightEvent flightEvent = subsequentEvents[subsequentEvents.Count - 1];
			if (flightEvent is EndFlightEvent)
			{
				return flightEvent.time;
			}
			else if (flightEvent is FinishMissionEvent)
			{
				return flightEvent.time;
			}
			else
			{
				return -1;
			}
		}

		internal object GetFinalMass()
		{
			if (subsequentEvents.Count == 0) return -1;
			if (GetEndDate() == -1) return currentMass;
			FlightEvent flightEvent = subsequentEvents[subsequentEvents.Count - 1];
			if (flightEvent is EndFlightEvent)
			{
				return (flightEvent as EndFlightEvent).finalMass;
			}
			else if (flightEvent is FinishMissionEvent)
			{
				return (flightEvent as FinishMissionEvent).finalMass;
			}
			else
			{
				return -1;
			}
		}

		internal float GetMassOnOrbit()
		{
			if (subsequentEvents.Count == 0) return -1;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is OrbitReachingEvent)
				{
					return (flightEvent as OrbitReachingEvent).massOnOrbit;
				}
			}
			return -1;
		}

		internal int GetLandingsCount()
		{
			int count = 0;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is LandingEvent)
				{
					count++;
				}
			}
			return count;
		}

		internal List<SOIChangeEvent> GetSOIChanges()
		{
			List<SOIChangeEvent> changes = new List<SOIChangeEvent>();
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is SOIChangeEvent)
				{
					changes.Add(flightEvent as SOIChangeEvent);
				}
			}
			return changes;
		}

		internal List<string> GetFinalCrew()
		{
			if (subsequentEvents.Count == 0) return new List<string>();
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is EndFlightEvent)
				{
					return (flightEvent as EndFlightEvent).crewMembers;
				}
				else if (flightEvent is FinishMissionEvent)
				{
					return (flightEvent as FinishMissionEvent).crewMembers;
				}
			}
			return new List<string>();
		}

		public int GetDockings()
		{
			int count = 0;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is DockingEvent)
				{
					count++;
				}
			}
			return count;
		}
		public int GetEvas()
		{
			int count = 0;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is EvaEvent)
				{
					count++;
				}
			}
			return count;
		}

		internal double GetEventMaxSpeed()
		{
			double maxSpeed = 0;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is MaxSpeedEvent)
				{
					maxSpeed = (flightEvent as MaxSpeedEvent).speed;
				}
			}
			return maxSpeed;
		}
		internal double GetEventMaxGee()
		{
			double gee = 0;
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is MaxGeeEvent)
				{
					gee = (flightEvent as MaxGeeEvent).gee;
				}
			}
			return gee;
		}

		internal float GetSciencePoints()
		{
			if (subsequentEvents.Count == 0) return -1;
			//FlightEvent flightEvent = subsequentEvents[subsequentEvents.Count - 1];
			/*if (flightEvent is EndFlightEvent)
			{
				return sciencePoints;
			}
			else
			{*/
				float points = 0;
				foreach (var item in subsequentEvents)
				{
					if (item is ScienceEvent)
						points += (item as ScienceEvent).sciencePoints;
				}
				return points;
			//}
		}

		internal void SetSciencePoints(float points)
		{
			if (subsequentEvents.Count == 0) return;
			sciencePoints = points;
		}

		internal IEnumerable<FlightEvent> GetLandings()
		{
			List<FlightEvent> landings = new List<FlightEvent>();
			for (int i = 0; i < subsequentEvents.Count; i++)
			{
				FlightEvent flightEvent = subsequentEvents[i];
				if (flightEvent is LandingEvent)
				{
					landings.Add(flightEvent);
				}
			}
			return landings;
		}

		internal bool IsDestroyed()
		{
			foreach (var item in subsequentEvents)
			{
				if (item is VesselDestroyedEvent)
					return true;
			}
			return false;
		}

		internal bool IsMissionFinished()
		{
			foreach (var item in subsequentEvents)
			{
				if (item is FinishMissionEvent)
					return true;
			}
			return false;
		}

		internal bool IsFlightEnded()
		{
			foreach (var item in subsequentEvents)
			{
				if (item is EndFlightEvent)
					return true;
			}
			return false;
		}
		public string GetTask()
		{
			string task = "atmospheric";
			foreach (var item in subsequentEvents)
			{
				if (item is SOIChangeEvent && subsequentEvents[0] != item)
				{
					task = "interplanetary";
					return task;
				}
				else if (item is OrbitReachingEvent && task == "atmospheric")
				{
					task = "suborbital";
				}
				else if (item is StableOrbitEvent)
				{
					task = "orbital";
				}
			}
			return task;
		}

		public ArrayList GetMasses()
		{
			ArrayList popupTexts = new ArrayList();

			string currentSoi = "Kerbin";
			foreach (var subEvent in subsequentEvents)
			{
				if (subEvent is SOIChangeEvent)
				{
					List<string> item = new List<string>();
					currentSoi = (subEvent as SOIChangeEvent).soiName;
					item.Add(currentSoi);
					item.Add(Utils.CorrectNumber((subEvent as SOIChangeEvent).mass.ToString(), 3));
					popupTexts.Add(item);
				}
				else if (subEvent is StableOrbitEvent)
				{
					List<string> item = new List<string>();
					item.Add(currentSoi+"-orbit");
					item.Add(Utils.CorrectNumber((subEvent as StableOrbitEvent).mass.ToString(), 3));
					popupTexts.Add(item);
				}
			}
			return popupTexts;
		}

		internal ArrayList GetBiomes()
		{
			ArrayList biomes = new ArrayList();
			foreach (var subEvent in subsequentEvents)
			{
				if (subEvent is LandingEvent)
				{
					List<string> item = new List<string>();
					item.Add((subEvent as LandingEvent).mainBodyName);
					item.Add((subEvent as LandingEvent).biome);
					biomes.Add(item);
				}
			}
			return biomes;
		}

		internal ArrayList GetExperiments()
		{
			ArrayList biomes = new ArrayList();
			foreach (var subEvent in subsequentEvents)
			{
				if (subEvent is ScienceEvent)
				{
					List<string> item = new List<string>();
					item.Add((subEvent as ScienceEvent).title);
					item.Add((subEvent as ScienceEvent).sciencePoints.ToString());
					biomes.Add(item);
				}
			}
			return biomes;
		}

		internal void checkCrew()
		{
			//TODO: check crew
		}
	}
}
