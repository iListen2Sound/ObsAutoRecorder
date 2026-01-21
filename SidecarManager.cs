using MelonLoader;
using OBS_Control_API;
using System.IO;
using RumbleModdingAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Video;
using Il2CppSteamworks;
using System.Threading.Tasks;
using Il2CppPlayFab.EconomyModels;
//using Il2CppSystem;
using UnityEngine.Rendering;

namespace ObsAutoRecorder
{

	public partial class ObsAutoRecorder : MelonMod
	{
		private List<string> Timestamps = new List<string>();
		private string TimeFileName { get; set; } = "";
		private string TempFileDir { get; set; } = "";

		private void AddNowStamp()
		{
			TimeFileName = LatestOutputPath;


			Task.Run(() =>
			{
				RequestResponse.GetRecordStatus req = OBS.GetRecordStatus();
				if (!req.outputActive)
				{
					Log($"AddNowStamp: Output not active.", true, 1);
					return;
				}

				//Check if TimeFileName has been assigned an output to associate with. If not, make a temp text file
				if (TimeFileName == "")
				{
					TimeFileName = $"Temp {AutoRenameString.Value}";
					string playerName = LastRecordedPlayer.Name;
					if (LastRecordedPlayer.ID == "0000000000000000" && misc.Value == 111)
					{
						playerName = $"Howard 2,147,483,647 BP";
					}

					string date = System.DateTime.Now.ToString(DateFormat.Value);
					string time = System.DateTime.Now.ToString(TimeFormat.Value);


					Log($"Player name for file rename: {LastRecordedPlayer.Name}");
					string mapName;
					switch (SceneName)
					{
						case "map0":
							mapName = "Ring";
							break;
						case "map1":
							mapName = "Pit";
							break;
						case "gym":
							mapName = "Gym";
							break;
						case "park":
							mapName = "Park";
							break;
						default:
							mapName = "Unknown";
							break;
					}
					TimeFileName = Path.Combine(TempFileDir, TimeFileName.Replace("{player}", $"{GetSafeFilename(playerName)}").Replace("{date}", date).Replace("{time}", time).Replace("{map}", mapName));

				}

				TimeFileName += ".txt";

				//Start retrieving recording data
				System.TimeSpan currentPlayTime = TimeSpan.FromMilliseconds(req.outputDuration);
				System.TimeSpan offsetDuration = TimeSpan.FromSeconds(TimestampOffset.Value);
				System.TimeSpan offsetTime = currentPlayTime - offsetDuration;

				offsetTime = offsetTime >= TimeSpan.Zero ? offsetTime : TimeSpan.Zero;

				string formattedEntry = TimestampFormat.Value
					.Replace("{timestamp}", currentPlayTime.ToString(TimecodeFormat.Value))
					.Replace("{offsetduration}", offsetDuration.ToString(TimecodeFormat.Value))
					.Replace("{offsettime}", offsetTime.ToString(TimecodeFormat.Value));
				Timestamps.Add(formattedEntry);


				File.WriteAllLines(TimeFileName, Timestamps);




			});
		}

	}
}