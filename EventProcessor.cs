using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	class EventProcessor
	{
		public static EventProcessor Instance = new EventProcessor();

		public List<LaunchEvent> launches = new List<LaunchEvent>();
		public List<LaunchCrewEvent> crewLaunches = new List<LaunchCrewEvent>();

		internal void UpdateMaxSpeed()
		{
			if (FlightGlobals.ActiveVessel == null) return;
			LaunchEvent activeLaunch = GetLaunch(FlightGlobals.ActiveVessel);
			if (activeLaunch != null)
			{
				ProtoVessel item = FlightGlobals.ActiveVessel.protoVessel;
				double currentVesselSpeed = item.vesselRef.obt_speed;
				if (item.situation == Vessel.Situations.FLYING || item.situation == Vessel.Situations.PRELAUNCH)
					currentVesselSpeed = item.vesselRef.srfSpeed;
				if (currentVesselSpeed > activeLaunch.maxSpeed)
					activeLaunch.maxSpeed = currentVesselSpeed;

				if (activeLaunch.GetLastEvent() is LandingEvent)
				{
					double altitude = FlightGlobals.ActiveVessel.RevealAltitude();
					if (altitude - FlightGlobals.ActiveVessel.terrainAltitude > 10)
						activeLaunch.AddEvent(new FlyingEvent());
				}
			}
		}
		public void RecordMaxSpeed()
		{
			if (FlightGlobals.ActiveVessel == null) return;
			LaunchEvent activeLaunch = GetLaunch(FlightGlobals.ActiveVessel);
			if (activeLaunch != null)
			{
				MaxSpeedEvent maxSpEv = new MaxSpeedEvent();
				maxSpEv.speed = activeLaunch.maxSpeed;
				activeLaunch.AddEvent(maxSpEv);
			}
		}
		public void OnLaunch(EventReport data)
		{
			Vessel vessel = FlightGlobals.ActiveVessel;
			if (vessel != null)
			{

				if (!vessel.isActiveVessel) return;
				if (vessel.missionTime <= 0.01)
				{
					RecordLaunch(vessel);
					RecordCrewLaunch(vessel);
				}
			}
		}

		private void RecordLaunch(Vessel vessel)
		{
			LaunchEvent launch = new LaunchEvent();
			launch.shipName = vessel.protoVessel.vesselName;
			launch.shipID = vessel.id.ToString();
			launch.rootPartID = vessel.rootPart.flightID.ToString();
			launch.time = GetTimeInTicks();
			launch.parts = new List<StatisticVehiclePart>();
			float sumCost = 0;
			float launchMass = 0;
			foreach (var part in vessel.parts)
			{
				sumCost += part.partInfo.cost;
				launchMass += part.GetResourceMass() + part.mass;
				StatisticVehiclePart vehiclePart = new StatisticVehiclePart();
				vehiclePart.partID = part.flightID.ToString();

				var modules = part.Modules.GetModules<ModuleCommand>();
				if (modules.Count > 0)
				{
					vehiclePart.partType = PartType.CommandPod;
					launch.parts.Add(vehiclePart);
				}
				else
				{
					vehiclePart.partType = PartType.Other;
					//launch.parts.Add(vehiclePart);
				}
			}
			launch.crewMembers = new List<string>();
			foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
			{
				launch.crewMembers.Add(kerbal.name);
			}
			launch.launchCost = sumCost;
			launch.launchMass = launchMass;

			SOIChangeEvent soiInitial = new SOIChangeEvent();
			soiInitial.soiName = vessel.mainBody.name;
			launch.AddEvent(soiInitial);

			launches.Add(launch);
		}
		private void RecordCrewLaunch(Vessel vessel)
		{
			long currentLaunchTime = GetTimeInTicks();
			foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
			{
				LaunchCrewEvent crewSubLaunch = new LaunchCrewEvent();
				crewSubLaunch.name = kerbal.name;
				crewSubLaunch.time = currentLaunchTime;

				LaunchCrewEvent crewLaunch = GetKerbalLaunch(kerbal.name);
				if (crewLaunch == null)
				{
					crewLaunches.Add(crewSubLaunch);
				}
				else
				{
					crewLaunch.AddEvent(crewSubLaunch);
				}
			}
		}
		private LaunchCrewEvent GetKerbalLaunch(string name)
		{
			foreach (LaunchCrewEvent crewLau in crewLaunches)
			{
				if (crewLau.name == name)
				{
					return crewLau;
				}
			}
			return null;
		}
		public void OnRecoveryProcessing(ProtoVessel data0, MissionRecoveryDialog data1, float data2)
		{
			foreach (var launch in launches)
			{
				if (data0.vesselID.ToString() == launch.shipID)
				{
					launch.SetSciencePoints(data1.scienceEarned);
				}
			}
			FlightGUI.SaveData();
		}

		public void OnVesselRecoveryRequested(Vessel vessel)
		{
			long currentEndTime = GetTimeInTicks();

			EndFlightEvent endFlight = new EndFlightEvent();
			endFlight.time = currentEndTime;
			endFlight.finalMass = 0;
			
			foreach (var part in vessel.parts)
			{
				endFlight.finalMass += part.GetResourceMass() + part.mass;
			}
			endFlight.crewMembers = new List<string>();
			foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
			{
				endFlight.crewMembers.Add(kerbal.name);
			}

			LaunchEvent launch = GetLaunch(vessel);
			launch.shipID = vessel.id.ToString();
			if(launch != null)
				launch.AddEvent(endFlight);

			foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
			{
				EndFlightCrewEvent crewEndFlight = new EndFlightCrewEvent();
				crewEndFlight.time = currentEndTime;
				GetKerbalLaunch(kerbal.name).AddEvent(crewEndFlight);
			}
			FlightGUI.SaveData();
		}
		private LaunchEvent GetLaunch(Vessel vessel)
		{
			if (vessel == null) return null;
			foreach (var part in vessel.parts)
			{
				var modules = part.Modules.GetModules<ModuleCommand>();
				if (modules.Count > 0)
				{
					foreach (var launch in launches)
					{
						foreach (var vehiclePart in launch.parts)
						{
							if (vehiclePart.partType == PartType.CommandPod && vehiclePart.partID == part.flightID.ToString())
							{
								launch.shipID = vessel.id.ToString();
								return launch;
							}
						}
					}
				}
			}
			return null;
		}
		private LaunchEvent GetLaunchByVesselId(string id)
		{
			foreach (var launch in launches)
			{
				if (id == launch.shipID)
					return launch;
			}
			return null;
		}

		private LaunchEvent GetLaunchByRootPartId(string partId)
		{
			foreach (var launch in launches)
			{
				if (launch.rootPartID == partId)
					return launch;
			}
			return null;
		}

		public void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
		{
			if (data.host.vesselType != VesselType.SpaceObject && data.host.vesselType != VesselType.Debris)
			{
				if (data.from == Vessel.Situations.FLYING && data.to == Vessel.Situations.SUB_ORBITAL)
					RecordOrbitReaching(data.host);

				if (data.from == Vessel.Situations.FLYING && (data.to == Vessel.Situations.LANDED ||
																data.to == Vessel.Situations.SPLASHED) && data.host.vesselType != VesselType.Debris)
				{
					RecordLanding(data.host);
				}
			}
		}
		public void RecordOrbitReaching(Vessel vessel)
		{
			OrbitReachingEvent orbitEvent = new OrbitReachingEvent();
			orbitEvent.massOnOrbit = 0;
			foreach (var part in vessel.parts)
			{
				orbitEvent.massOnOrbit += part.GetResourceMass() + part.mass;
			}
			LaunchEvent launch = GetLaunch(vessel);
			if (launch != null)
				launch.AddEvent(orbitEvent);

		}

		public void RecordLanding(Vessel vessel)
		{
			if (vessel.isEVA) return;
			LandingEvent landing = new LandingEvent();
			landing.mainBodyName = vessel.mainBody.name;
			LaunchEvent launch = GetLaunch(vessel);
			if (launch != null)
			{
				FlightEvent flightEvent = launch.GetLastEvent();
				if (flightEvent is EvaEvent && (landing.time - flightEvent.time) / TimeSpan.TicksPerSecond < 1) return;
				if (flightEvent is LandingEvent)
				{
					if (((landing.time - flightEvent.time) / TimeSpan.TicksPerSecond < 2))
						return;
				}
				launch.AddEvent(landing);
			}


			foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
			{
				LandingCrewEvent landingCrewEvent = new LandingCrewEvent();
				LaunchCrewEvent crewLaunch = GetKerbalLaunch(kerbal.name);
				crewLaunch.AddEvent(landingCrewEvent);
			}
		}

		public static long GetTimeInTicks()
		{
			return (long)(Planetarium.GetUniversalTime() * TimeSpan.TicksPerSecond);
		}

		public void OnVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> data)
		{
			SOIChangeEvent soiChange = new SOIChangeEvent();
			soiChange.soiName = data.host.mainBody.name;
			LaunchEvent launch = GetLaunch(data.host);
			if (launch != null)
				launch.AddEvent(soiChange);
		}

		internal void OnCrewOnEva(GameEvents.FromToAction<Part, Part> data)
		{
			if (VesselType.EVA != data.to.vessel.vesselType)
				return;
			LaunchCrewEvent crewLaunch = GetKerbalLaunch(data.to.vessel.vesselName);
			EvaCrewEvent evaCrewEvent = new EvaCrewEvent();
			crewLaunch.AddEvent(evaCrewEvent);

			LaunchEvent launch = GetLaunch(data.from.vessel);
			if (launch != null)
			{
				EvaEvent evaEvent = new EvaEvent();
				launch.AddEvent(evaEvent);
			}
		}

		internal void OnCrewBoardVessel(GameEvents.FromToAction<Part, Part> data)
		{
			LaunchCrewEvent crewLaunch = GetKerbalLaunch(data.from.vessel.vesselName);
			EvaCrewEndEvent evaCrewEndEvent = new EvaCrewEndEvent();
			crewLaunch.AddEvent(evaCrewEndEvent);
		}

		public void OnPartCouple(GameEvents.FromToAction<Part, Part> action)
		{
			if (!HighLogic.LoadedSceneIsFlight) return;
			Part from = action.from;
			Part to = action.to;

			if (from == null || from.vessel == null || from.vessel.isEVA || from.vessel.parts.Count == 1) return;
			if (to == null || to.vessel == null || to.vessel.isEVA || to.vessel.parts.Count == 1) return;

			Vessel vessel = action.from.vessel.isActiveVessel ? action.from.vessel : action.to.vessel;
			if (action.from.vessel != null && action.to.vessel != null)
			{
				DockingEvent dockingFrom = new DockingEvent();
				LaunchEvent launchFrom = GetLaunch(action.from.vessel);
				if(launchFrom != null)
					launchFrom.AddEvent(dockingFrom);

				DockingEvent dockingTo = new DockingEvent();
				LaunchEvent launchTo = GetLaunch(action.to.vessel);
				if (launchTo != null)
					launchTo.AddEvent(dockingTo);

				RecordCrewDockingEvent(action.from.vessel, action.to.vessel);
			}
		}

		private void RecordCrewDockingEvent(Vessel from, Vessel to)
		{
			foreach (ProtoCrewMember kerbal in from.GetVesselCrew())
			{
				DockingCrewEvent landingCrewEvent = new DockingCrewEvent();
				LaunchCrewEvent crewLaunch = GetKerbalLaunch(kerbal.name);
				crewLaunch.AddEvent(landingCrewEvent);
			}
			foreach (ProtoCrewMember kerbal in to.GetVesselCrew())
			{
				DockingCrewEvent landingCrewEvent = new DockingCrewEvent();
				LaunchCrewEvent crewLaunch = GetKerbalLaunch(kerbal.name);
				crewLaunch.AddEvent(landingCrewEvent);
			}
		}



		internal void Revert()
		{
			bool removed = false;
			long currentTime = GetTimeInTicks();
			if (currentTime <= 10000000) return;
			int removedCount = launches.RemoveAll(x => x.time > currentTime);
			foreach (var launch in launches)
			{
				removed = launch.Revert(currentTime);
			}

			removedCount += crewLaunches.RemoveAll(x => x.time > currentTime);
			foreach (var kerbal in crewLaunches)
			{
				removed = kerbal.Revert(currentTime) || removed;
			}

			if (FlightGlobals.ActiveVessel == null) return;
			LaunchEvent activeLaunch = GetLaunch(FlightGlobals.ActiveVessel);
			if (activeLaunch != null)
			{
				activeLaunch.maxSpeed = activeLaunch.GetEventMaxSpeed();
			}
		}

		internal void OnEndFlight(ProtoVessel protoVessel)
		{
			LaunchEvent launch = GetLaunchByVesselId(protoVessel.vesselID.ToString());
			if (launch == null || launch.GetLastEvent() is EndFlightEvent) return;
			EndFlightEvent endFlight = new EndFlightEvent();
			endFlight.finalMass = 0;
			foreach (var part in protoVessel.protoPartSnapshots)
			{
				endFlight.finalMass += part.mass;
			}
			endFlight.crewMembers = new List<string>();
			foreach (var kerbal in protoVessel.GetVesselCrew())
				endFlight.crewMembers.Add(kerbal.name);
			launch.AddEvent(endFlight);
			FlightGUI.SaveData();
		}

		internal string GetTotalCost()
		{
			float cost = 0;
			foreach (var item in launches)
			{
				cost += item.launchCost;
			}
			return cost.ToString();
		}

		internal string GetTotalLaunches()
		{
			return launches.Count.ToString();
		}

		internal long GetTotalTimePilots()
		{
			long time = 0;
			foreach (var item in launches)
			{
				if(item.GetLastEvent() is EndFlightEvent && item.crewMembers.Count > 0)
					time += item.GetTotalFlightTime();
			}
			return time;
		}
		internal long GetTotalTimeBots()
		{
			long time = 0;
			foreach (var item in launches)
			{
				if (item.GetLastEvent() is EndFlightEvent && item.crewMembers.Count == 0)
					time += item.GetTotalFlightTime();
			}
			return time;
		}

		internal void OnCrash(EventReport data)
		{
			AttachNode node1 = data.origin.attachNodes[0];
			AttachNode node2;
			if( data.origin.attachNodes.Count > 1)
				node2 = data.origin.attachNodes[1];
			LaunchEvent launch = GetLaunchByRootPartId(data.origin.flightID.ToString());
			for (int i = 0; i < data.origin.attachNodes.Count && launch == null; i++)
			{
				launch = GetLaunchByRootPartId(data.origin.attachNodes[i].attachedPartId.ToString());
			}
			if (launch != null)
			{
				VesselDestroyedEvent destroyed = new VesselDestroyedEvent();
				launch.AddEvent(destroyed);

				EndFlightEvent endFlight = new EndFlightEvent();
				endFlight.finalMass = 0;
				endFlight.crewMembers = new List<string>();
				launch.AddEvent(endFlight);
			}
			
		}



		internal void OnScienceReceived(float data0, ScienceSubject data1, ProtoVessel data2, bool data3)
		{
			LaunchEvent launch = GetLaunchByVesselId(data2.vesselID.ToString());
			ScienceEvent scienceEvent = new ScienceEvent();
			scienceEvent.sciencePoints = data0;
			launch.AddEvent(scienceEvent);
		}

		internal void OnVesselTerminated(ProtoVessel data)
		{
			LaunchEvent launch = GetLaunchByVesselId(data.vesselID.ToString());
			if (launch != null)
			{
				VesselDestroyedEvent destroyed = new VesselDestroyedEvent();
				launch.AddEvent(destroyed);

				EndFlightEvent endFlight = new EndFlightEvent();
				float sumMass = 0;
				foreach (var part in data.protoPartSnapshots)
				{
					sumMass += part.mass;
				}
				endFlight.finalMass = sumMass;
				endFlight.crewMembers = new List<string>();
				foreach (ProtoCrewMember kerbal in data.GetVesselCrew())
				{
					endFlight.crewMembers.Add(kerbal.name);
				}
				
				launch.AddEvent(endFlight);
			}
		}
	}
}
