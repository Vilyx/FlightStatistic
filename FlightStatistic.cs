using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Toolbar;
using UnityEngine;

namespace OLDD
{
	[KSPAddon(KSPAddon.Startup.Instantly, false)]
	public class FlightStatistic : MonoBehaviour
	{
		private static readonly String RESOURCE_PATH = "OLDD/FlightStatistic/Resource/";

		void Awake()
		{
			if (FlightGUI.Instance == null)
			{
				GameObject gui = new GameObject();
				gui.AddComponent<FlightGUI>();
				GameObject.DontDestroyOnLoad(gui);
			}
			String iconOff = RESOURCE_PATH + "IconOff_24";
			var toolbarButton = ToolbarManager.Instance.add("FlightStatistic", "FlightStatistic");
			if (toolbarButton != null)
			{
				toolbarButton.TexturePath = iconOff;
				toolbarButton.ToolTip = "Open Flight Statistic";
				toolbarButton.OnClick += (e) =>
				{
					FlightGUI.Instance.IsStatisticOpened = !FlightGUI.Instance.IsStatisticOpened;
				};

				toolbarButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION);
			}
		}

	}
}