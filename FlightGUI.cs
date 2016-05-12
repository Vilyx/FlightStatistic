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

		private static readonly String ROOT_PATH = Utils.GetRootPath();
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
		private Vector2 shipsScrollViewVector;
		private bool showMannedLaunches = true;
		private bool showUnmannedLaunches = true;
		private bool showMissionFinished = true;
		private bool showEnded = true;
		private bool showDestroyed = true;
		private bool showInAction = true;
		private bool reverseSortLaunches = true;
		private SortLaunchesParameter sortLaunchesParameter = SortLaunchesParameter.DATE;
		private GUIContent hintContent = new GUIContent("Button 1", "Button 1");

		private int popupWindowWidth = 200;
		private Rect popupWindowRect = new Rect(20, 80, 10, 10);
		private bool showPopup = false;
		private ArrayList popupTexts = new ArrayList();
		private string hover;
		

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
			int someInt = 12;
			GUI.skin.label.fontSize = someInt;
			GUI.skin.button.fontSize = someInt;
			GUI.skin.box.fontSize = someInt;
			if (isStatisticOpened)
			{
				mainWindowRect = GUI.Window(1001, mainWindowRect, DrawMainWindow, "Statistic");
				if (showPopup) popupWindowRect = GUILayout.Window(1006, popupWindowRect, DrawPopupWindow, "");
				if (needToShowFlight && toolbarInt == 0)
				{
					flightWindowRect = GUI.Window(1002, flightWindowRect, DrawFlightWindow, "");
				}
				else if (needToShowKerbal && toolbarInt == 1)
				{
					kerbalWindowRect = GUI.Window(1003, kerbalWindowRect, DrawKerbalWindow, "");
				}
			}
			ProcessPopup();
			GUI.skin.label.fontSize = temp;
			GUI.skin.button.fontSize = temp;
			GUI.skin.box.fontSize = temp;

			EventProcessor.Instance.UpdateDynamic();
		}
		private void ProcessPopup()
		{
			

			string newHover = GUI.tooltip;
			if (newHover != "")
			{
				Debug.Log(newHover);
			}
			if (Event.current.type == EventType.Repaint && newHover != hover)
			{
				hover = newHover;
				if (hover == "Masses")
				{
					LaunchEvent launch = EventProcessor.Instance.launches[flightIdToShow];
					popupTexts = launch.GetMasses();
					showPopup = true;
				}
				else if (hover == "Biomes")
				{
					LaunchEvent launch = EventProcessor.Instance.launches[flightIdToShow];
					popupTexts = launch.GetBiomes();
					showPopup = true;
				}
				else if (hover == "Science points")
				{
					LaunchEvent launch = EventProcessor.Instance.launches[flightIdToShow];
					popupTexts = launch.GetExperiments();
					showPopup = true;
				}
				else
				{
					showPopup = false;
				}
				popupWindowRect.height = 1;
				popupWindowRect.width = 1;
			}
			popupWindowRect.x = Input.mousePosition.x;
			popupWindowRect.y = Screen.height - Input.mousePosition.y;
		}
		private void DrawPopupWindow(int windowID)
		{
			for (int i = 0; i < popupTexts.Count; i++)
			{
				GUILayout.BeginHorizontal(GUILayout.Width(popupWindowWidth));
				if (popupTexts[i] is List<string>)
				{
					foreach (string str in (popupTexts[i] as List<string>))
						GUILayout.Label(str);
				}
				else
				{
					GUILayout.Label(popupTexts[i].ToString());
				}
				GUILayout.EndHorizontal();
			}
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
			GameEvents.onVesselRename.Add(EventProcessor.Instance.OnVesselRename);

			GameEvents.onCrewBoardVessel.Add(EventProcessor.Instance.OnCrewChanged);
			GameEvents.onCrewOnEva.Add(EventProcessor.Instance.OnCrewChanged);
			GameEvents.onCrewKilled.Add(EventProcessor.Instance.OnCrewChanged);
			GameEvents.OnCrewmemberHired.Add(EventProcessor.Instance.OnCrewChanged);
			GameEvents.OnCrewmemberLeftForDead.Add(EventProcessor.Instance.OnCrewChanged);
			GameEvents.OnCrewmemberSacked.Add(EventProcessor.Instance.OnCrewChanged);
			GameEvents.onCrewTransferred.Add(EventProcessor.Instance.OnCrewChanged);

			//GameEvents.onVesselTerminated.Add(EventProcessor.Instance.OnVesselTerminated);
			GameEvents.onCrewKilled.Add(EventProcessor.Instance.OnCrewKilled);
			GameEvents.onCrash.Add(EventProcessor.Instance.OnCrash);
			GameEvents.onVesselTerminated.Add(EventProcessor.Instance.OnVesselTerminated);
			GameEvents.onCrashSplashdown.Add(EventProcessor.Instance.OnCrash);
			GameEvents.onVesselSituationChange.Add(EventProcessor.Instance.OnVesselSituationChange);
			GameEvents.onVesselSOIChanged.Add(EventProcessor.Instance.OnVesselSOIChanged);
			GameEvents.onPartCouple.Add(EventProcessor.Instance.OnPartCouple);
			GameEvents.onPartUndock.Add(EventProcessor.Instance.OnPartUndock);
			GameEvents.onStageSeparation.Add(EventProcessor.Instance.OnStageSeparation);

			GameEvents.onGameStateCreated.Add(OnGameStateCreated);
			GameEvents.onGameStateLoad.Add(OnGameStateLoad);
			GameEvents.onGameStateSave.Add(OnGameStateSave);
		}
		private void OnGameStateCreated(Game game)
		{
			if (game != null)
			{
				Debug.Log("FlightStatistic::OnGameStateCreated gameTitle=" + game.Title);
				String name = game.Title;
				if (name != null && name.Length>0)
					Utils.userName = name.Substring(0, name.IndexOf("(") - 1);
			}
			LoadData();
			EventProcessor.Instance.Revert();
		}
		private void OnGameStateLoad(ConfigNode data)
		{
			if (data != null)
			{
				String name = data.GetValue("Title");
				if(name != null)
					Utils.userName = name.Substring(0, name.IndexOf("(") - 1);
				Debug.Log("FlightStatistic::OnGameStateLoad userName=" + Utils.userName);
			}

			LoadData();
			EventProcessor.Instance.Revert();
		}
		private void LoadData()
		{
			EventProcessor.Instance.crewLaunches = null;
			EventProcessor.Instance.launches = null;
			String savePath = SAVE_BASE_FOLDER + Utils.userName + "/";
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
			String savePath = SAVE_BASE_FOLDER + Utils.userName + "/";
			Debug.Log("FlightStatistic::SaveData savePath=" + savePath + "statistic_new.json");
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
			if (System.IO.File.Exists(savePath + "statistic_new.json"))
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
			DrawKerbalsColumn(kerbals, "Total flight time", "GetTotalFlightTime", (object obj) => { return Utils.TicksToTotalTime(obj.ToString()); });

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
			currentStyle.fontStyle = FontStyle.Bold;
			currentStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(captionRect, txt, currentStyle);
			currentStyle.fontStyle = FontStyle.Normal;
			currentStyle.alignment = TextAnchor.MiddleLeft;

			//количество полетов, длительность (накопительно), количество выходов в открытый космос и их общая длительность (накопительно), количество стыковок,  количество посадок.
			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical();
			GUILayout.Label("Kerbal name", boldtext);
			GUILayout.Label("Flights", boldtext);
			GUILayout.Label("Max Gee", boldtext);
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
			GUILayout.Label(Utils.CorrectNumber(kerbal.maxGee.ToString()) + " G");
			GUILayout.Label(Utils.TicksToTotalTime(kerbal.GetTotalFlightTime()));
			GUILayout.Label(Utils.TicksToTotalTime(kerbal.GetTotalEvasTime()));
			GUILayout.Label(kerbal.GetEvasCount().ToString());
			GUILayout.Label(kerbal.GetDockingsCount().ToString());
			GUILayout.Label(kerbal.GetLandingsCount().ToString());
			GUILayout.Label(Utils.TicksToTotalTime(kerbal.GetIdleTime()));
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

			ProcessPopup();
			GUI.DragWindow(new Rect(0, 0, 10000, 20));
		}

		private void DrawFlights()
		{
			float divideCoef = 6.5f;
			// Begin the ScrollView
			scrollViewVector = GUILayout.BeginScrollView(scrollViewVector, GUIStyle.none, GUI.skin.verticalScrollbar);
			GUILayout.BeginHorizontal();
			showMannedLaunches = GUILayout.Toggle(showMannedLaunches, "Manned", GUILayout.Width(mainWindowRect.width / divideCoef));
			if (!showMannedLaunches && !showUnmannedLaunches) showUnmannedLaunches = true;
			showUnmannedLaunches = GUILayout.Toggle(showUnmannedLaunches, "Unmanned", GUILayout.Width(mainWindowRect.width / divideCoef));
			if (!showMannedLaunches && !showUnmannedLaunches) showMannedLaunches = true;
			showMissionFinished = GUILayout.Toggle(showMissionFinished, "Finished", GUILayout.Width(mainWindowRect.width / divideCoef));
			showEnded = GUILayout.Toggle(showEnded, "Ended", GUILayout.Width(mainWindowRect.width / divideCoef));
			showDestroyed = GUILayout.Toggle(showDestroyed, "Destroyed", GUILayout.Width(mainWindowRect.width / divideCoef));
			showInAction = GUILayout.Toggle(showInAction, "InAction", GUILayout.Width(mainWindowRect.width / divideCoef));
			GUILayout.EndHorizontal();
			List<LaunchEvent> launches = EventProcessor.Instance.launches.GetRange(0, EventProcessor.Instance.launches.Count);
			for (int i = 0; i < launches.Count; i++)
			{
				launches[i].posInTable = i+1;
			}

			launches = filterLaunches(launches);
			launches = sortLaunches(launches);
			/*if (reverseFlights)
				launches.Reverse();*/

			GUILayout.BeginHorizontal();
			DrawFlightsColumn(0,launches, "№", "posInTable", (object obj) => { return obj.ToString(); });
			DrawFlightsColumn(1,launches, "Vessel", "shipName", (object obj) => { return obj.ToString(); });
			DrawFlightsColumn(4,launches, "Task", "GetTask", (object obj) => { return obj.ToString(); });
			DrawFlightsColumn(2,launches, "Launch date", "time", (object obj) => { return Utils.TicksToDate(obj.ToString()); });
			DrawFlightsColumn(3,launches, "Cost", "launchCost", (object obj) => { return Utils.CorrectNumber(obj.ToString()); });
			GUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.EndScrollView();
			DrawTotals();
		}

		private List<LaunchEvent> sortLaunches(List<LaunchEvent> launches)
		{
			if(sortLaunchesParameter == SortLaunchesParameter.DATE)
				launches = launches.OrderBy(x => x.time).ToList();
			else if (sortLaunchesParameter == SortLaunchesParameter.NAME)
				launches = launches.OrderBy(x => x.shipName).ToList();
			else if (sortLaunchesParameter == SortLaunchesParameter.COST)
				launches = launches.OrderBy(x => x.launchCost).ToList();
			else if (sortLaunchesParameter == SortLaunchesParameter.TASK)
				launches = launches.OrderBy(x => x.GetTask()).ToList();
			if(reverseSortLaunches)
				launches.Reverse();
			return launches;
		}

		private List<LaunchEvent> filterLaunches(List<LaunchEvent> launches)
		{
			List<LaunchEvent> le = new List<LaunchEvent>();
			/*if (mouseOverLineNum == i)
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
					GUILayout.Label(txt, proceedingStyle);*/
			foreach (var item in launches)
			{
				if ((showMannedLaunches && item.crewMembers.Count > 0 || showUnmannedLaunches && item.crewMembers.Count == 0) &&
					(showMissionFinished && item.IsMissionFinished() || showEnded && item.GetEndDate() != -1 || showDestroyed && item.IsDestroyed() || item.GetEndDate() == -1 && showInAction))
				{
					le.Add(item);
				}
			}
			return le;
		}

		private void DrawFlightsColumn(int sortIndex,List<LaunchEvent> launches, string columnName, string propName, Func<object, string> propProc)
		{
			GUILayout.BeginVertical();
			GUI.color = normalColor;
			//GUILayout.Box("Vessel");
			if (GUILayout.Button(columnName, GUILayout.Height(15)))
			{
				if (sortIndex == 0 || sortIndex == 2)
				{
					if (sortLaunchesParameter == SortLaunchesParameter.DATE)
						reverseSortLaunches = !reverseSortLaunches;
					else
						reverseSortLaunches = false;
					sortLaunchesParameter = SortLaunchesParameter.DATE;
				}
				else if (sortIndex == 1)
				{
					if (sortLaunchesParameter == SortLaunchesParameter.NAME)
						reverseSortLaunches = !reverseSortLaunches;
					else
						reverseSortLaunches = false;
					sortLaunchesParameter = SortLaunchesParameter.NAME;
				}
				else if (sortIndex == 3)
				{
					if (sortLaunchesParameter == SortLaunchesParameter.COST)
						reverseSortLaunches = !reverseSortLaunches;
					else
						reverseSortLaunches = false;
					sortLaunchesParameter = SortLaunchesParameter.COST;
				}
				else if (sortIndex == 4)
				{
					if (sortLaunchesParameter == SortLaunchesParameter.TASK)
						reverseSortLaunches = !reverseSortLaunches;
					else
						reverseSortLaunches = false;
					sortLaunchesParameter = SortLaunchesParameter.TASK;
				}
			}

			for (int i = 0; i < launches.Count; i++)
			{
				GUILayout.BeginHorizontal();
				//GUILayout.FlexibleSpace();
				Type type = launches[i].GetType();
				FieldInfo pinfo = type.GetField(propName);
				string txt = "";
				if (pinfo != null)
				{
					txt = propProc(pinfo.GetValue(launches[i]));
				}
				else
				{
					MethodInfo minfo = type.GetMethod(propName);
					var res = minfo.Invoke(launches[i], null);
					txt = propProc(res.ToString());
				}

				if (txt == null) txt = "";

				if (mouseOverLineNum == i)
					GUILayout.Label(txt, selectedStyle);
				else if (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.id.ToString() == launches[i].shipID)
					GUILayout.Label(txt, activeStyle);
				else if (launches[i].IsDestroyed())
					GUILayout.Label(txt, destroyedStyle);
				else if (launches[i].GetEndDate() != -1 && launches[i].IsFlightEnded())
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
							ClickedFlight(launches[i].posInTable - 1);
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
			/*if (reverseFlights)
				launches.Reverse();*/
			LaunchEvent launch = launches[flightIdToShow];
			if (!(launch.IsMissionFinished() || launch.GetLastEvent() is EndFlightEvent))
			{
				if (GUI.Button(new Rect(5, flightWindowRect.height - 22, 100, 20), "Finish mission"))
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
			currentStyle.fontStyle = FontStyle.Bold;
			currentStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Label(captionRect, txt, currentStyle);
			currentStyle.fontStyle = FontStyle.Normal;
			currentStyle.alignment = TextAnchor.MiddleLeft;

			

			GUILayout.BeginHorizontal();
			GUILayout.BeginVertical(GUILayout.Width(100));
			GUILayout.Label("Vessel", boldtext);
			GUILayout.Label("Launch date", boldtext);
			GUILayout.Label("Launch mass", boldtext);
			GUILayout.Label("Cost", boldtext);
			hintContent.text = "Close Orbit/SOI masses";
			hintContent.tooltip = "Masses";
			GUILayout.Label(hintContent, boldtext);
			GUILayout.Label("Docked", boldtext);
			GUILayout.Label("EVAs", boldtext);
			GUILayout.Label("Max speed", boldtext);
			GUILayout.Label("Max Gee", boldtext);
			hintContent.text = "Landings";
			hintContent.tooltip = "Biomes";
			GUILayout.Label(hintContent, boldtext);
			GUILayout.Label("End date", boldtext);
			GUILayout.Label("Duration", boldtext);
			GUILayout.Label("Final mass", boldtext);
			hintContent.text = "Science points";
			hintContent.tooltip = "Science points";
			GUILayout.Label(hintContent, boldtext);
			GUILayout.EndVertical();

			GUILayout.BeginVertical(GUILayout.Width(400));
			GUILayout.Label(launch.shipName);
			GUILayout.Label(Utils.TicksToDate(launch.time));
			GUILayout.Label(Utils.CorrectNumber(launch.launchMass.ToString(), 3));
			GUILayout.Label(Utils.CorrectNumber(launch.launchCost.ToString()));
			float massOnOrbit = launch.GetMassOnOrbit();
			string massOnOrbitText = "-";
			if (massOnOrbit > 0) massOnOrbitText = massOnOrbit.ToString();
			hintContent.text = Utils.CorrectNumber(massOnOrbitText);
			hintContent.tooltip = "Masses";
			GUILayout.Label(hintContent);
			GUILayout.Label(launch.GetDockings().ToString());
			GUILayout.Label(launch.GetEvas().ToString());
			GUILayout.Label(Utils.CorrectNumber(launch.maxSpeed.ToString()) + "m/s");
			GUILayout.Label(Utils.CorrectNumber(launch.maxGee.ToString()) + " G");
			hintContent.text = launch.GetLandingsCount().ToString();
			hintContent.tooltip = "Biomes";
			GUILayout.Label(hintContent);

			long endDate = launch.GetEndDate();
			if (endDate != -1)
			{
				GUILayout.Label(Utils.TicksToDate(endDate));
				GUILayout.Label(Utils.TicksToTotalTime(launch.GetTotalFlightTime()));
			}
			else
			{
				GUILayout.Label("-");
				GUILayout.Label(Utils.TicksToTotalTime(launch.GetTotalFlightTime()));
			}
			GUILayout.Label(Utils.CorrectNumber(launch.GetFinalMass().ToString(), 3));
			float sciencePoints = launch.GetSciencePoints();
			string sciencePointsText = "-";
			if (sciencePoints > 0) sciencePointsText = Utils.CorrectNumber(sciencePoints.ToString());
			hintContent.text = sciencePointsText;
			hintContent.tooltip = "Science points";
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
					GUILayout.Label(Utils.TicksToDate(record.time) + " \t " + (record as SOIChangeEvent).soiName.ToString());
				else
					GUILayout.Label(Utils.TicksToDate(record.time) + " \t Land:" + (record as LandingEvent).mainBodyName.ToString() 
						+ ":" + (record as LandingEvent).biome.ToString());
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

			ProcessPopup();
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
			GUILayout.BeginVertical();
			GUILayout.Label("Total cost: " + EventProcessor.Instance.GetTotalCost(), GUILayout.Height(10));
			GUILayout.Label("manned: " + EventProcessor.Instance.GetTotalCostPilots(), GUILayout.Height(10));
			GUILayout.Label("unmanned: " + EventProcessor.Instance.GetTotalCostBots(), GUILayout.Height(10));
			GUILayout.EndVertical();
			GUILayout.BeginVertical();
			GUILayout.Label("Total launches: " + EventProcessor.Instance.GetTotalLaunches(), GUILayout.Height(10));
			GUILayout.Label("manned: " + EventProcessor.Instance.GetTotalTimesPilots(), GUILayout.Height(10));
			GUILayout.Label("unmanned: " + EventProcessor.Instance.GetTotalTimesBots(), GUILayout.Height(10));
			GUILayout.EndVertical();
			GUILayout.BeginVertical();
			GUILayout.Label("Total time manned: " + Utils.TicksToTotalTime(EventProcessor.Instance.GetTotalTimePilots()), GUILayout.Height(10));
			GUILayout.Label("             unmanned: " + Utils.TicksToTotalTime(EventProcessor.Instance.GetTotalTimeBots()), GUILayout.Height(10));
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}


	}
}
