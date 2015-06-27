using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Toolbar;
using UnityEngine;

namespace OLDD
{
	class FlightGUI : MonoBehaviour
	{
		public static FlightGUI Instance;
		private bool isStatisticOpened = false;

		public bool IsStatisticOpened
		{
			get { return isStatisticOpened; }
			set
			{
				isStatisticOpened = value;
				if (!isStatisticOpened)
				{
					needToShowFlight = false;
					needToShowKerbal = false;
				}
			}
		}

		private Vector2 scrollViewVector = new Vector2();
		private Vector2 scrollViewVector1 = new Vector2();
		private Vector2 scrollViewVector2 = new Vector2();
		private Vector2 scrollViewVector3 = new Vector2();

		private Rect openBtnWindowRect = new Rect(20, 20, 70, 85);
		private Rect mainWindowRect = new Rect(20, 80, 600, 500);

		private int mouseOverLineNum = 0;

		private Color32 normalColor = new Color32(255, 255, 255, 255);
		private Color32 highlightColor = new Color32(255, 255, 0, 255);
		private Color32 backgroundColor = new Color32(0, 0, 0, 255);

		private GUIStyle boldtext;

		public int toolbarInt = 0;
		public string[] toolbarStrings = new string[] { "Flights", "Kerbals" };
		private int flightIdToShow = 0;
		private bool needToShowFlight = false;
		private int kerbalIdToShow = 0;
		private bool needToShowKerbal = false;

		private Rect flightWindowRect = new Rect(40, 100, 800, 500);
		private Rect kerbalWindowRect = new Rect(40, 100, 800, 500);

		private static readonly String ROOT_PATH = GetRootPath();
		private static readonly String SAVE_BASE_FOLDER = ROOT_PATH + "/saves/"; // suggestion/hint from Cydonian Monk
		private static readonly String RESOURCE_PATH = "OLDD/FlightStatistic/Resource/";
		private GUIStyle activeStyle;
		private GUIStyle selectedStyle;
		private GUIStyle endedStyle;
		private GUIStyle missionFinishedStyle;
		private GUIStyle destroyedStyle;
		private GUIStyle proceedingStyle;
		private int mouseOverLineNumKerb;
		private bool useNativeGui = true;
		private bool reverseFlights = true;
		private Vector2 shipsScrollViewVector;

		void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
				InitEventListeners();
			}
		}


		void OnGUI()
		{
			int temp = GUI.skin.label.fontSize;
			if (useNativeGui)
				GUI.skin = HighLogic.Skin;

			if (activeStyle == null)
			{
				activeStyle = new GUIStyle(GUI.skin.label);
				activeStyle.normal.textColor = Color.yellow;
				selectedStyle = new GUIStyle(GUI.skin.label);
				selectedStyle.normal.textColor = Color.white;
				//selectedStyle.fontSize += 2;
				endedStyle = new GUIStyle(GUI.skin.label);
				endedStyle.normal.textColor = Color.green;
				destroyedStyle = new GUIStyle(GUI.skin.label);
				destroyedStyle.normal.textColor = Color.red;
				missionFinishedStyle = new GUIStyle(GUI.skin.label);
				missionFinishedStyle.normal.textColor = Color.blue;
				proceedingStyle = new GUIStyle(GUI.skin.label);
				proceedingStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
			}
			//style.normal.textColor = Color.red;

			boldtext = new GUIStyle(GUI.skin.label);
			boldtext.fontStyle = FontStyle.Bold;
			boldtext.wordWrap = false;
			//myCustomStyle.clipping = TextClipping.clip;
			int someInt = 14;
			GUI.skin.label.fontSize = someInt;
			GUI.skin.button.fontSize = someInt;
			GUI.skin.box.fontSize = someInt;
			if (isStatisticOpened)
			{
				mainWindowRect = GUI.Window(1001, mainWindowRect, DrawMainWindow, "Statistic");
				if (needToShowFlight && toolbarInt == 0)
				{
					flightWindowRect = GUI.Window(1002, flightWindowRect, DrawFlightWindow, "");
				}
				else if (needToShowKerbal && toolbarInt == 1)
				{
					kerbalWindowRect = GUI.Window(1003, kerbalWindowRect, DrawKerbalWindow, "");
				}
			}
			GUI.skin.label.fontSize = temp;
			GUI.skin.button.fontSize = temp;
			GUI.skin.box.fontSize = temp;

			EventProcessor.Instance.UpdateMaxSpeed();
		}
		private void InitEventListeners()
		{
			GameEvents.onLaunch.Add(EventProcessor.Instance.OnLaunch);
			GameEvents.OnVesselRecoveryRequested.Add(EventProcessor.Instance.OnVesselRecoveryRequested);
			GameEvents.onVesselRecovered.Add(EventProcessor.Instance.OnEndFlight);
			GameEvents.onVesselRecoveryProcessing.Add(EventProcessor.Instance.OnRecoveryProcessing);
			GameEvents.onCrewOnEva.Add(EventProcessor.Instance.OnCrewOnEva);
			GameEvents.onCrewBoardVessel.Add(EventProcessor.Instance.OnCrewBoardVessel);
			GameEvents.OnScienceRecieved.Add(EventProcessor.Instance.OnScienceReceived);

			//GameEvents.onVesselTerminated.Add(EventProcessor.Instance.OnVesselTerminated);
			GameEvents.onCrewKilled.Add(EventProcessor.Instance.OnCrewKilled);
			GameEvents.onCrash.Add(EventProcessor.Instance.OnCrash);
			GameEvents.onVesselTerminated.Add(EventProcessor.Instance.OnVesselTerminated);
			GameEvents.onCrashSplashdown.Add(EventProcessor.Instance.OnCrash);
			GameEvents.onVesselSituationChange.Add(EventProcessor.Instance.OnVesselSituationChange);
			GameEvents.onVesselSOIChanged.Add(EventProcessor.Instance.OnVesselSOIChanged);
			GameEvents.onPartCouple.Add(EventProcessor.Instance.OnPartCouple);

			GameEvents.onGameStateCreated.Add(OnGameStateCreated);
			GameEvents.onGameStateLoad.Add(OnGameStateLoad);
			GameEvents.onGameStateSave.Add(OnGameStateSave);
		}
		private void OnGameStateCreated(Game game)
		{
			// no game, no fun
			if (game == null)
			{
				return;
			}
			LoadData();
			EventProcessor.Instance.Revert();
		}
		private void OnGameStateLoad(ConfigNode data)
		{
			LoadData();
			EventProcessor.Instance.Revert();
		}
		private void LoadData()
		{
			EventProcessor.Instance.crewLaunches = null;
			EventProcessor.Instance.launches = null;

			String savePath = SAVE_BASE_FOLDER + HighLogic.SaveFolder + "/";
			if (File.Exists(savePath + "statistic.json"))
			{
				using (StreamReader sr = new StreamReader(File.Open(savePath + "statistic.json", FileMode.Open), Encoding.UTF8))
				{
					try
					{
						string json = sr.ReadToEnd();
						if (json.Trim().Length > 2)
						{
							DataHolder dataHolder = (DataHolder)MyJsonUtil.JsonToObject<DataHolder>(json);
							EventProcessor.Instance.crewLaunches = dataHolder.crewLaunches;
							EventProcessor.Instance.launches = dataHolder.launches;
							useNativeGui = dataHolder.useNativeGui;
						}
						else
						{
							EventProcessor.Instance.crewLaunches = new List<LaunchCrewEvent>();
							EventProcessor.Instance.launches = new List<LaunchEvent>();
						}
					}
					catch
					{
						EventProcessor.Instance.crewLaunches = new List<LaunchCrewEvent>();
						EventProcessor.Instance.launches = new List<LaunchEvent>();
					}
				}
			}
			else
			{
				EventProcessor.Instance.crewLaunches = new List<LaunchCrewEvent>();
				EventProcessor.Instance.launches = new List<LaunchEvent>();
			}
		}
		private void OnGameStateSave(ConfigNode data)
		{
			SaveData();
		}
		public static void SaveData()
		{
			EventProcessor.Instance.OnBeforeSave();
			String savePath = SAVE_BASE_FOLDER + HighLogic.SaveFolder + "/";
			using (StreamWriter sw = new StreamWriter(File.Open(savePath + "statistic_new.json", FileMode.OpenOrCreate), Encoding.UTF8))
			{
				DataHolder dataHolder = new DataHolder();
				dataHolder.crewLaunches = EventProcessor.Instance.crewLaunches;
				dataHolder.launches = EventProcessor.Instance.launches;
				string json = MyJsonUtil.ObjectToJson(dataHolder);
				sw.WriteLine(json);
			}
			if (System.IO.File.Exists(savePath + "statistic_old.json"))
				System.IO.File.Delete(savePath + "statistic_old.json");
			if (System.IO.File.Exists(savePath + "statistic.json"))
				System.IO.File.Move(savePath + "statistic.json", savePath + "statistic_old.json");
			System.IO.File.Move(savePath + "statistic_new.json", savePath + "statistic.json");
		}
		void DrawMainWindow(int windowID)
		{
			if (GUI.Button(new Rect(mainWindowRect.width - 28, 2, 25, 16), "X"))
			{
				IsStatisticOpened = !IsStatisticOpened;
			}
			if (GUI.Button(new Rect(mainWindowRect.width - 56, 2, 25, 16), "S"))
			{
				useNativeGui = !useNativeGui;
			}
			toolbarInt = GUILayout.Toolbar(toolbarInt, toolbarStrings);

			if (toolbarInt == 1)
			{
				DrawKerbals();
			}
			else
			{
				DrawFlights();
			}

			GUI.DragWindow(new Rect(0, 0, 10000, 20));
		}
		void DrawKerbals()
		{
			// Begin the ScrollView
			scrollViewVector = GUILayout.BeginScrollView(scrollViewVector);
			List<LaunchCrewEvent> kerbals = EventProcessor.Instance.crewLaunches;
			for (int i = 0; i < kerbals.Count; i++)
			{
				kerbals[i].posInTable = i+1;
			}

			GUILayout.BeginHorizontal();
			DrawKerbalsColumn(kerbals, "№", "posInTable", (object obj) => { return obj.ToString(); });
			DrawKerbalsColumn(kerbals, "Kerbal name", "name", (object obj) => { return obj.ToString(); });
			DrawKerbalsColumn(kerbals, "Flights", "GetLaunchesCount", (object obj) => { return obj.ToString(); });
			DrawKerbalsColumn(kerbals, "Total flight time", "GetTotalFlightTime", (object obj) => { return TicksToTotalTime(obj.ToString()); });

			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.EndScrollView();
			DrawTotals();
		}
		private void DrawKerbalsColumn(List<LaunchCrewEvent> kerbals, string columnName, string propName, Func<object, string> propProc)
		{
			GUILayout.BeginVertical();
			GUI.color = normalColor;
			GUILayout.Box(columnName);

			for (int i = 0; i < kerbals.Count; i++)
			{

				if (mouseOverLineNumKerb == i) GUI.color = highlightColor;
				else if (GUI.color != normalColor) GUI.color = normalColor;

				GUILayout.BeginHorizontal();
				//GUILayout.FlexibleSpace();
				Type type = kerbals[i].GetType();
				FieldInfo pinfo = type.GetField(propName);
				string txt;
				if (pinfo != null)
				{
					txt = propProc(pinfo.GetValue(kerbals[i]));
				}
				else
				{
					MethodInfo minfo = type.GetMethod(propName);
					var res = minfo.Invoke(kerbals[i], null);
					txt = propProc(res.ToString());
				}
				
				
				if (mouseOverLineNumKerb == i)
					GUILayout.Label(txt, selectedStyle);
				else if (!kerbals[i].IsAlive())
					GUILayout.Label(txt, destroyedStyle);
				else
					GUILayout.Label(txt, proceedingStyle);
				//GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				Rect guiRect = GUILayoutUtility.GetLastRect();
				if (Event.current != null && Event.current.isMouse && Input.GetMouseButtonDown(0))
				{
					bool overGUI = guiRect.Contains(Event.current.mousePosition);
					if (overGUI)
					{
						mouseOverLineNumKerb = i;
						if(Event.current.clickCount == 2)
							ClickedKerbal(i);
					}
				}
			}
			GUILayout.EndVertical();
		}
		private void ClickedKerbal(int kerbalId)
		{
			Debug.Log("Kerbal clicked:" + kerbalId.ToString());
			kerbalIdToShow = kerbalId;
			needToShowKerbal = true;
		}
		private void DrawKerbalWindow(int id)
		{
			if (GUI.Button(new Rect(kerbalWindowRect.width - 28, 2, 25, 16), "X"))
			{
				needToShowKerbal = false;
			}
			LaunchCrewEvent kerbal = EventProcessor.Instance.crewLaunches[kerbalIdToShow];

			Rect captionRect = new Rect(0, 2, kerbalWindowRect.width, 16);
			string txt = "Kerbal: " + kerbal.name;
			GUIStyle currentStyle;
			if (!kerbal.IsAlive())
				currentStyle = destroyedStyle;
			else
				currentStyle = proceedingStyle;
			currentStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(captionRect, txt, currentStyle);
			currentStyle.alignment = TextAnchor.MiddleLeft;

			//количество полетов, длительность (накопительно), количество выходов в открытый космос и их общая длительность (накопительно), количество стыковок,  количество посадок.
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			GUILayout.Label("Kerbal name", boldtext);
			GUILayout.Label("Flights", boldtext);
			GUILayout.Label("Total flight time", boldtext);
			GUILayout.Label("EVAs time", boldtext);
			GUILayout.Label("EVAs", boldtext);
			GUILayout.Label("Dockings", boldtext);
			GUILayout.Label("Landings", boldtext);
			GUILayout.Label("Idle time", boldtext);
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			
			GUILayout.Label(kerbal.name);
			GUILayout.Label(kerbal.GetLaunchesCount().ToString());
			GUILayout.Label(TicksToTotalTime(kerbal.GetTotalFlightTime()));
			GUILayout.Label(TicksToTotalTime(kerbal.GetTotalEvasTime()));
			GUILayout.Label(kerbal.GetEvasCount().ToString());
			GUILayout.Label(kerbal.GetDockingsCount().ToString());
			GUILayout.Label(kerbal.GetLandingsCount().ToString());
			GUILayout.Label(TicksToTotalTime(kerbal.GetIdleTime()));
			GUILayout.EndVertical();
			GUILayout.BeginVertical();
			GUILayout.Box("Ships");
			shipsScrollViewVector = GUILayout.BeginScrollView(shipsScrollViewVector, GUILayout.Height((kerbalWindowRect.height - 130)));
			foreach (string shipName in kerbal.GetShips())
			{
				GUILayout.Label(shipName);
			}
			GUILayout.EndScrollView();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUI.DragWindow(new Rect(0, 0, 10000, 20));
		}

		private void DrawFlights()
		{
			// Begin the ScrollView
			scrollViewVector = GUILayout.BeginScrollView(scrollViewVector);
			List<LaunchEvent> launches = EventProcessor.Instance.launches.GetRange(0, EventProcessor.Instance.launches.Count);
			for (int i = 0; i < launches.Count; i++)
			{
				launches[i].posInTable = i+1;
			}
			if (reverseFlights)
				launches.Reverse();

			GUILayout.BeginHorizontal();
			DrawFlightsColumn(launches, "№", "posInTable", (object obj) => { return obj.ToString(); });
			DrawFlightsColumn(launches, "Vessel", "shipName", (object obj) => { return obj.ToString(); });
			DrawFlightsColumn(launches, "Launch date", "time", (object obj) => { return TicksToDate(obj.ToString()); });
			DrawFlightsColumn(launches, "Cost", "launchCost", (object obj) => { return CorrectNumber(obj.ToString()); });
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.EndScrollView();
			DrawTotals();
		}

		private void DrawFlightsColumn(List<LaunchEvent> launches, string columnName, string propName, Func<object, string> propProc)
		{
			GUILayout.BeginVertical();
			GUI.color = normalColor;
			//GUILayout.Box("Vessel");
			if (GUILayout.Button(columnName, GUILayout.Height(15)))
				reverseFlights = !reverseFlights;

			for (int i = 0; i < launches.Count; i++)
			{
				GUILayout.BeginHorizontal();
				//GUILayout.FlexibleSpace();
				Type type = launches[i].GetType();
				FieldInfo pinfo = type.GetField(propName);
				string txt = propProc(pinfo.GetValue(launches[i]));
				if (mouseOverLineNum == i)
					GUILayout.Label(txt, selectedStyle);
				else if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.id.ToString() == launches[i].shipID)
					GUILayout.Label(txt, activeStyle);
				else if (launches[i].IsDestroyed())
					GUILayout.Label(txt, destroyedStyle);
				else if (launches[i].GetEndDate() != -1)
					GUILayout.Label(txt, endedStyle);
				else if (launches[i].IsMissionFinished())
					GUILayout.Label(txt, missionFinishedStyle);
				else
					GUILayout.Label(txt, proceedingStyle);
				//GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				Rect guiRect = GUILayoutUtility.GetLastRect();

				if (Event.current != null && Event.current.isMouse && Input.GetMouseButtonDown(0))
				{
					bool overGUI = guiRect.Contains(Event.current.mousePosition);
					if (overGUI)
					{
						mouseOverLineNum = i;
						if(Event.current.clickCount == 2)
							ClickedFlight(i);
					}
				}
			}
			GUILayout.EndVertical();
		}

		private void DrawFlightWindow(int id)
		{
			if (GUI.Button(new Rect(flightWindowRect.width - 28, 2, 25, 16), "X"))
			{
				needToShowFlight = false;
			}
			
			
			List<LaunchEvent> launches = EventProcessor.Instance.launches.GetRange(0, EventProcessor.Instance.launches.Count);
			if (reverseFlights)
				launches.Reverse();
			LaunchEvent launch = launches[flightIdToShow];
			if (!(launch.IsMissionFinished() || launch.GetLastEvent() is EndFlightEvent))
			{
				if (GUI.Button(new Rect(5, 2, 100, 16), "Finish mission"))
				{
					EventProcessor.Instance.OnFinishMission(launch);
				}
			}
			Rect captionRect = new Rect(0, 2, flightWindowRect.width, 16);
			string txt = "Flight №" + launch.posInTable + " - " + launch.shipName;
			GUIStyle currentStyle;
			if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.id.ToString() == launch.shipID)
				currentStyle = activeStyle;
			else if (launch.IsDestroyed())
				currentStyle = destroyedStyle;
			else if (launch.GetEndDate() != -1)
				currentStyle = endedStyle;
			else
				currentStyle = proceedingStyle;
			currentStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(captionRect, txt, currentStyle);
			currentStyle.alignment = TextAnchor.MiddleLeft;

			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical(GUILayout.Width(100));
			GUILayout.Label("Vessel", boldtext);
			GUILayout.Label("Launch date", boldtext);
			GUILayout.Label("Launch mass", boldtext);
			GUILayout.Label("Cost", boldtext);
			GUILayout.Label("Mass on orbit", boldtext);
			GUILayout.Label("Docked", boldtext);
			GUILayout.Label("EVAs", boldtext);
			GUILayout.Label("Max speed", boldtext);
			GUILayout.Label("Landings", boldtext);
			GUILayout.Label("End date", boldtext);
			GUILayout.Label("Duration", boldtext);
			GUILayout.Label("Final mass", boldtext);
			GUILayout.Label("Science points", boldtext);
			GUILayout.EndVertical();

			GUILayout.BeginVertical(GUILayout.Width(400));
			GUILayout.Label(launch.shipName);
			GUILayout.Label(TicksToDate(launch.time));
			GUILayout.Label(CorrectNumber(launch.launchMass.ToString(), 3));
			GUILayout.Label(CorrectNumber(launch.launchCost.ToString()));
			float massOnOrbit = launch.GetMassOnOrbit();
			string massOnOrbitText = "-";
			if (massOnOrbit > 0) massOnOrbitText = massOnOrbit.ToString();
			GUILayout.Label(CorrectNumber(massOnOrbitText));
			GUILayout.Label(launch.GetDockings().ToString());
			GUILayout.Label(launch.GetEvas().ToString());
			GUILayout.Label(CorrectNumber(launch.maxSpeed.ToString()) + "m/s");
			GUILayout.Label(launch.GetLandingsCount().ToString());

			long endDate = launch.GetEndDate();
			if (endDate != -1)
			{
				GUILayout.Label(TicksToDate(endDate));
				GUILayout.Label(TicksToTotalTime(launch.GetTotalFlightTime()));
				GUILayout.Label(CorrectNumber(launch.GetFinalMass().ToString(), 3));
			}
			else
			{
				GUILayout.Label("-");
				GUILayout.Label(TicksToTotalTime(launch.GetTotalFlightTime()));
				GUILayout.Label("-");
			}
			float sciencePoints = launch.GetSciencePoints();
			string sciencePointsText = "-";
			if (sciencePoints > 0) sciencePointsText = CorrectNumber(sciencePoints.ToString());
			GUILayout.Label(sciencePointsText);
			GUILayout.EndVertical();

			GUILayout.BeginVertical();
			GUILayout.Box("SOI changes (dates, SOI names)");
			scrollViewVector1 = GUILayout.BeginScrollView(scrollViewVector1, GUILayout.Height((flightWindowRect.height - 130) / 3));
			List<FlightEvent> sois = launch.GetSOIChanges().ToArray().ToList<FlightEvent>();
			sois.AddRange(launch.GetLandings());
			sois.Sort();
			foreach (FlightEvent record in sois)
			{
				if(record is SOIChangeEvent)
					GUILayout.Label(TicksToDate(record.time) + " \t " + (record as SOIChangeEvent).soiName.ToString());
				else if((record as LandingEvent).mainBodyName != "Kerbin")
					GUILayout.Label(TicksToDate(record.time) + " \t Land:" + (record as LandingEvent).mainBodyName.ToString());
			}
			GUILayout.EndScrollView();
			GUILayout.Box("Initial crew");
			scrollViewVector2 = GUILayout.BeginScrollView(scrollViewVector2, GUILayout.Height((flightWindowRect.height - 130) / 3));
			foreach (string crewMember in launch.crewMembers)
			{
				GUILayout.Label(crewMember);
			}
			GUILayout.EndScrollView();
			GUILayout.Box("Final crew");
			scrollViewVector3 = GUILayout.BeginScrollView(scrollViewVector3, GUILayout.Height((flightWindowRect.height - 130) / 3));
			foreach (string crewMember in launch.GetFinalCrew())
			{
				GUILayout.Label(crewMember);
			}
			GUILayout.EndScrollView();

			GUILayout.EndVertical();
			GUILayout.BeginVertical();
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
			GUI.DragWindow(new Rect(0, 0, 10000, 20));
		}

		private void ClickedFlight(int flightId)
		{
			flightIdToShow = flightId;
			needToShowFlight = true;
		}
		private void DrawTotals()
		{
			GUI.color = normalColor;
			GUILayout.BeginHorizontal();
			//общая стоимость (увеличивается с каждым полетом), общее количество полетов, общая длительность.
			GUILayout.Label("Total cost: " + EventProcessor.Instance.GetTotalCost() + "\t");
			GUILayout.Label("Total launches: " + EventProcessor.Instance.GetTotalLaunches() + "\t");
			GUILayout.Label("Total time manned: " + TicksToTotalTime(EventProcessor.Instance.GetTotalTimePilots()) + "  unmanned: " + TicksToTotalTime(EventProcessor.Instance.GetTotalTimeBots()));
			GUILayout.EndHorizontal();
		}

		protected string CorrectNumber(string num, int cAD = 2)
		{
			string strToRet = num;
			int commaIndex = strToRet.IndexOf(".");
			if (commaIndex != -1 && strToRet.Length > commaIndex + cAD + 1)
			{
				strToRet = strToRet.Substring(0, commaIndex + cAD + 1);
			}
			else if (commaIndex == -1)
			{
				commaIndex = strToRet.Length;
			}
			for (int i = commaIndex - 3; i > 0; i -= 3)
				strToRet = strToRet.Insert(i, " ");
			return strToRet;
		}
		private string TicksToDate(object p)
		{
			if (p == null) return "-";
			/*60 Kerbin seconds per Kerbin minute
			60 Kerbin minutes per Kerbin hour
			6 Kerbin hours per Kerbin day
			6.43 Kerbin days per Kerbin month
			426.08 Kerbin days per Kerbin year*/
			double seconds = long.Parse(p.ToString()) / TimeSpan.TicksPerSecond;
			long minutes = (long)(seconds / 60);
			long hours = (long)(minutes / 60);
			long days = (long)(hours / 6);
			long years = (long)(days / 426.08);

			string yearsStr = EnlargeByZeroes((years + 1).ToString(), 2);
			string daysStr = EnlargeByZeroes(((long)(days - years * 426.08) + 1).ToString(), 2);
			string hoursStr = EnlargeByZeroes((hours - days * 6).ToString(), 2);
			string minutesStr = EnlargeByZeroes((minutes - hours * 60).ToString(), 2);
			string secondsStr = EnlargeByZeroes((seconds - minutes * 60).ToString(), 2);
			return yearsStr + "." + daysStr + "/" + hoursStr + ":" + minutesStr + ":" + secondsStr;
		}
		private string TicksToTotalTime(object p)
		{
			if (p == null) return "-";
			long ticks = long.Parse(p.ToString());
			long days = ticks / (TimeSpan.TicksPerDay/4);
			long hours = ticks % (TimeSpan.TicksPerDay/4) / TimeSpan.TicksPerHour;
			long minutes = ticks % TimeSpan.TicksPerHour / TimeSpan.TicksPerMinute;
			long seconds = ticks % TimeSpan.TicksPerMinute / TimeSpan.TicksPerSecond;
			return "D" + days.ToString() + ", " + hours.ToString() + ":" + EnlargeByZeroes(minutes.ToString(), 2) + ":" + EnlargeByZeroes(seconds.ToString(), 2);
		}
		private string EnlargeByZeroes(string source, int minLen)
		{
			while (source.Length < minLen)
				source = "0" + source;
			return source;
		}
		public static String GetRootPath()
		{
			String path = KSPUtil.ApplicationRootPath;
			path = path.Replace("\\", "/");
			if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
			//
			return path;
		}
	}
}
