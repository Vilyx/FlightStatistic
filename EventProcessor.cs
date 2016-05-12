using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP.UI.Screens;
using UnityEngine;

namespace OLDD
{
	class EventProcessor
	{
		public static EventProcessor Instance = new EventProcessor();

		public List<LaunchEvent> launches = new List<LaunchEvent>();
		public List<LaunchCrewEvent> crewLaunches = new List<LaunchCrewEvent>();

		internal void UpdateDynamic()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;
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

				if (activeLaunch.maxGee < FlightGlobals.ActiveVessel.geeForce)
					activeLaunch.maxGee = FlightGlobals.ActiveVessel.geeForce;
				foreach (ProtoCrewMember kerbal in FlightGlobals.ActiveVessel.GetVesselCrew())
				{
					LaunchCrewEvent crewLaunch = GetKerbalLaunch(kerbal.name);
					if (crewLaunch.maxGee < FlightGlobals.ActiveVessel.geeForce)
					{
						crewLaunch.maxGee = FlightGlobals.ActiveVessel.geeForce;
					}
				}

				if (activeLaunch.GetLastEvent() is LandingEvent)
				{
					double altitude = FlightGlobals.ActiveVessel.RevealAltitude();
					if (altitude - FlightGlobals.ActiveVessel.terrainAltitude > 10)
						activeLaunch.AddEvent(new FlyingEvent());
				}
				float currentMass = 0;
				foreach (var part in FlightGlobals.ActiveVessel.parts)
				{
					currentMass += part.GetResourceMass() + part.mass;
				}
				activeLaunch.currentMass = currentMass;
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
				if(activeLaunch.GetEventMaxSpeed() < activeLaunch.maxSpeed)
					activeLaunch.AddEvent(maxSpEv);
			}
		}
		public void RecordMaxGee()
		{
			if (FlightGlobals.ActiveVessel == null) return;
			LaunchEvent activeLaunch = GetLaunch(FlightGlobals.ActiveVessel);
			if (activeLaunch != null)
			{
				MaxGeeEvent maxGeeEv = new MaxGeeEvent();
				maxGeeEv.gee = activeLaunch.maxGee;
				if(activeLaunch.maxGee > activeLaunch.GetEventMaxGee())
					activeLaunch.AddEvent(maxGeeEv);

				foreach (ProtoCrewMember kerbal in FlightGlobals.ActiveVessel.GetVesselCrew())
				{
					LaunchCrewEvent crewLaunch = GetKerbalLaunch(kerbal.name);
					if (crewLaunch.maxGee < activeLaunch.maxGee)
					{
						MaxGeeCrewEvent geeEv = new MaxGeeCrewEvent();
						geeEv.gee = activeLaunch.maxGee;
						if(crewLaunch.GetEventMaxGee() < activeLaunch.maxGee)
							crewLaunch.AddEvent(geeEv);
					}
				}
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
			if (launch.shipName == null) launch.shipName = "Just decoupled";
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
			soiInitial.mass = launchMass;
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
				crewSubLaunch.vesselName = vessel.vesselName;

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

		internal void OnEndFlight(ProtoVessel protoVessel, bool data1)
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

		internal void OnRecoveryProcessing(ProtoVessel data0, MissionRecoveryDialog data1, float data2)
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
				if (data.to == Vessel.Situations.ORBITING)
					RecordStableOrbit(data.host);

				if ((data.from == Vessel.Situations.FLYING || data.from == Vessel.Situations.SUB_ORBITAL)
				    && (data.to == Vessel.Situations.LANDED || data.to == Vessel.Situations.SPLASHED) 
				    && data.host.vesselType != VesselType.Debris)
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
			landing.biome = getBiomeName(vessel.mainBody, vessel.longitude, vessel.latitude);
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
		private int getBiomeIndex(CelestialBody body, double lon, double lat)
		{
			if (body.BiomeMap == null) return -1;

			CBAttributeMapSO.MapAttribute att = body.BiomeMap.GetAtt(Mathf.Deg2Rad * lat, Mathf.Deg2Rad * lon);
			for (int i = 0; i < body.BiomeMap.Attributes.Length; ++i)
			{
				if (body.BiomeMap.Attributes[i] == att)
				{
					return i;
				}
			}
			return -1;
		}
		internal double fixLat(double lat)
		{
			return (lat + 180 + 90) % 180;
		}

		internal double fixLon(double lon)
		{
			return (lon + 360 + 180) % 360;
		}
		internal CBAttributeMapSO.MapAttribute getBiome(CelestialBody body, double lon, double lat)
		{
			if (body.BiomeMap == null) return null;
			int i = getBiomeIndex(body, lon, lat);
			if (i == -1)
				return null;
			return body.BiomeMap.Attributes[i];
		}

		internal string getBiomeName(CelestialBody body, double lon, double lat)
		{
			CBAttributeMapSO.MapAttribute a = getBiome(body, lon, lat);
			if (a == null)
				return "unknown";
			return a.name;
		}
		public static long GetTimeInTicks()
		{
			return (long)(Planetarium.GetUniversalTime() * TimeSpan.TicksPerSecond);
		}

		public void OnVesselSOIChanged(GameEvents.HostedFromToAction<Vessel, CelestialBody> data)
		{
			SOIChangeEvent soiChange = new SOIChangeEvent();
			soiChange.mass = 0;
			foreach (var part in data.host.parts)
			{
				soiChange.mass += part.GetResourceMass() + part.mass;
			}
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
				activeLaunch.maxGee = activeLaunch.GetEventMaxGee();
			}
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
		internal string GetTotalCostPilots()
		{
			float cost = 0;
			foreach (var item in launches)
			{
				if (item.crewMembers.Count > 0)
					cost += item.launchCost;
			}
			return cost.ToString();
		}
		internal string GetTotalCostBots()
		{
			float cost = 0;
			foreach (var item in launches)
			{
				if (item.crewMembers.Count == 0)
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
				if ((item.GetLastEvent() is EndFlightEvent || item.IsMissionFinished()) && item.crewMembers.Count > 0)
					time += item.GetTotalMissionTime();
			}
			return time;
		}
		internal long GetTotalTimeBots()
		{
			long time = 0;
			foreach (var item in launches)
			{
				if ((item.GetLastEvent() is EndFlightEvent || item.IsMissionFinished()) && item.crewMembers.Count == 0)
					time += item.GetTotalMissionTime();
			}
			return time;
		}
		internal string GetTotalTimesPilots()
		{
			long times = 0;
			foreach (var item in launches)
			{
				if (item.crewMembers.Count > 0)
					times++;
			}
			return times.ToString();
		}
		internal string GetTotalTimesBots()
		{
			long times = 0;
			foreach (var item in launches)
			{
				if (item.crewMembers.Count == 0)
					times++;
			}
			return times.ToString();
		}

		internal void OnCrash(EventReport data)
		{
			LaunchEvent launch = null;
			foreach (var part in data.origin.attachNodes)
			{
				if (launch != null) break;
				launch = GetLaunchByRootPartId(data.origin.flightID.ToString());
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
			scienceEvent.title = data1.title;
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

		public void OnFinishMission(LaunchEvent launch)
		{
			FinishMissionEvent endMission = new FinishMissionEvent();
			Vessel[] vessels = GameObject.FindObjectsOfType<Vessel>();
			Vessel vessel = null;
			foreach (var item in vessels)
			{
				if (item.id.ToString() == launch.shipID && item.vesselName == launch.shipName)
					vessel = item;
			}
			if (vessel == null) return;
			float sumMass = 0;
			foreach (var part in vessel.protoVessel.protoPartSnapshots)
			{
				sumMass += part.mass;
			}
			endMission.finalMass = sumMass;
			endMission.crewMembers = new List<string>();
			foreach (ProtoCrewMember kerbal in vessel.protoVessel.GetVesselCrew())
			{
				endMission.crewMembers.Add(kerbal.name);
			}
			launch.AddEvent(endMission);
		}

		internal void OnCrewKilled(EventReport data)
		{
			string kerbalName = data.sender;
			DeathCrewEvent death = new DeathCrewEvent();
			LaunchCrewEvent kerbal = GetKerbalLaunch(kerbalName);
			if(kerbal != null)
				kerbal.AddEvent(death);
		}

		internal void OnBeforeSave()
		{
			if (FlightGlobals.ActiveVessel == null) return;
			var state = FlightGlobals.ActiveVessel.state;
			LaunchEvent launch = GetLaunchByVesselId(FlightGlobals.ActiveVessel.id.ToString());
			if (launch != null)
			{
				RecordMaxSpeed();
				RecordMaxGee();
			}
			if (launch != null && state == Vessel.State.DEAD)
			{
				if (launch.GetLastEvent() is EndFlightEvent) return;
				VesselDestroyedEvent destroyed = new VesselDestroyedEvent();
				launch.AddEvent(destroyed);

				EndFlightEvent endFlight = new EndFlightEvent();
				endFlight.finalMass = 0;
				endFlight.crewMembers = new List<string>();
				launch.AddEvent(endFlight);
			}
		}

		private void RecordStableOrbit(Vessel vessel)
		{
			LaunchEvent launch = GetLaunch(vessel);
			if (launch != null)
			{
 				StableOrbitEvent stableEvent = new StableOrbitEvent();
				stableEvent.mass = 0;
				foreach (var part in vessel.parts)
				{
					stableEvent.mass += part.GetResourceMass() + part.mass;
				}

				launch.AddEvent(stableEvent);
			}
			/*if (FlightGlobals.ActiveVessel == null) return;
			FlightGlobals.ActiveVessel.*/
		}

		internal void OnVesselRename(GameEvents.HostedFromToAction<Vessel, string> data)
		{
			LaunchEvent launch = GetLaunch(data.host);
			if (launch != null)
			{
				launch.shipName = data.to;
			}

		}

		internal void OnPartUndock(Part data)
		{
			OnVesselSeparate(data.vessel);
		}

		internal void OnStageSeparation(EventReport data)
		{
			foreach (var ves in FlightGlobals.Vessels)
			{
				if (ves.missionTime < 0.01)
					OnVesselSeparate(ves);
			}
		}
		internal void OnVesselSeparate(Vessel vessel)
		{
			LaunchEvent launch = GetLaunch(vessel);
			int cmdPodsCount = 0;
			foreach (var part in vessel.parts)
			{
				var modules = part.Modules.GetModules<ModuleCommand>();
				if (modules.Count > 0)
				{
					cmdPodsCount++;
				}
			}
			if (launch != null && launch.parts.Count > cmdPodsCount)
			{
				foreach (var part in vessel.parts)
				{
					var modules = part.Modules.GetModules<ModuleCommand>();
					if (modules.Count > 0)
					{
						launch.parts.RemoveAll((StatisticVehiclePart statisticPart) => { return statisticPart.partID == part.flightID.ToString(); });
					}
				}
				RecordLaunch(vessel);
			}
		}

		internal void OnCrewChanged(GameEvents.FromToAction<Part, Part> evaBoardData)
		{
			LaunchEvent launch = GetLaunch(evaBoardData.from.vessel);
			if (launch != null)
			{
				launch.checkCrew();
			}
			launch = GetLaunch(evaBoardData.to.vessel);
			if (launch != null)
			{
				launch.checkCrew();
			}
		}

		internal void OnCrewChanged(EventReport killedData)
		{
			LaunchEvent launch = GetLaunch(killedData.origin.vessel);
			if (launch != null)
			{
				launch.checkCrew();
			}
			//throw new NotImplementedException();
		}

		internal void OnCrewChanged(ProtoCrewMember hiredLeftSackedData, int data1)
		{
			//hiredLeftSackedData.
		}

		internal void OnCrewChanged(GameEvents.HostedFromToAction<ProtoCrewMember, Part> transferredData)
		{
			LaunchEvent launch = GetLaunch(transferredData.from.vessel);
			if (launch != null)
			{
				launch.checkCrew();
			}
			launch = GetLaunch(transferredData.to.vessel);
			if (launch != null)
			{
				launch.checkCrew();
			}
		}
	}
}
