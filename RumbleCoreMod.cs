using Il2CppPhoton.Realtime;
using Il2CppRUMBLE;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Social;
using Il2CppRUMBLE.UI;
using Il2CppTMPro;
using JetBrains.Annotations;
using MelonLoader;
using OBS_Control_API;
using System.IO;
using RumbleModdingAPI;
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
using static OBS_Control_API.RequestResponse;


[assembly: MelonInfo(typeof(ObsAutoRecorder.ObsAutoRecorder), ObsAutoRecorder.BuildInfo.Name, ObsAutoRecorder.BuildInfo.Version, ObsAutoRecorder.BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonAuthorColor(255, 87, 166, 80)]
[assembly: MelonColor(255, 87, 166, 80)]

namespace ObsAutoRecorder
{
	public static class BuildInfo
	{
		public const string Name = "ObsAutoRecorder";
		public const string Author = "iListen2Sound";
		public const string Version = "1.0.0";
	}
	public partial class ObsAutoRecorder : MelonMod
	{
		private string lastLogDiff;

		//Hold button location 
		//--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Settings Screen/InteractionButton (1)/
		private const string USER_DATA = "UserData/ObsAutoRecorder/";
		private const string CONFIG_FILE = "config.cfg";
		private const string RECORD_LIST = "AutoRecordList.txt";
		private const string SEPARATOR = "\n";
		public static ObsAutoRecorder Instance { get; private set; }

		string SceneName { get; set; }
		private MelonPreferences_Category OBSAutoRecorderSettings;
		private MelonPreferences_Entry<bool> isDebugMode;

		private MelonPreferences_Category AutoRenameSettings;
		//private MelonPreferences_Entry<string> PlayersToRecord;
		private MelonPreferences_Entry<string> AutoRenameString;
		private MelonPreferences_Entry<bool> DoAutoRename;
		private MelonPreferences_Entry<string> DateFormat;
		private MelonPreferences_Entry<string> TimeFormat;
		private MelonPreferences_Entry<string> ReplayPrefix;

		private MelonPreferences_Category RecordingSettings;
		private MelonPreferences_Entry<bool> AddChapterMarkers;
		private MelonPreferences_Entry<int> RecordingPauseHoldTimeout;
		private MelonPreferences_Entry<int> RecordByBPThreshold;
		private MelonPreferences_Entry<bool> PauseAfterMatch;

		private MelonPreferences_Category IndicatorSettings;
		private MelonPreferences_Entry<bool> PreferMinimalIcon;
		private MelonPreferences_Entry<bool> ClippingIconVisibleByDefault;
		private MelonPreferences_Entry<bool> RockCamVisibility;


		private List<string> AutoRecordList { get; set; } = new();



		private static GameObject IndicatorsBase;
		RequestResponse.GetRecordStatus getRecordStatus = new();





		private object _debounceCor = null;
		private object _pollTagsCor = null;
		private object _pollPageCor = null;

		//private object _recordingWaitCor = null;
		public static GameObject GetIndicator()
		{
			return GameObject.Instantiate(IndicatorsBase);
		}
		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			SceneName = sceneName.ToLower();

		}
		public override void OnApplicationQuit()
		{
			if (!ExternalRecording)
			{
				Log("Game Closing. Forcing recording stop");
				RequestRecordingStop();
			}
			OBSAutoRecorderSettings.SaveToFile();
		}

		public override void OnInitializeMelon()
		{

			if (!Directory.Exists(USER_DATA))
				Directory.CreateDirectory(USER_DATA);

			if (!File.Exists(Path.Combine(USER_DATA, RECORD_LIST)))
				File.Create(Path.Combine(USER_DATA, RECORD_LIST));

			OBSAutoRecorderSettings = MelonPreferences.CreateCategory("ObsAutoRecorder");
			OBSAutoRecorderSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			isDebugMode = OBSAutoRecorderSettings.CreateEntry("Debug Mode", false, null, "Enable debug with more verbose logging");

			
			AutoRenameSettings = MelonPreferences.CreateCategory("Auto Rename Settings");
			AutoRenameSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			DoAutoRename = AutoRenameSettings.CreateEntry("Enable Auto Rename", true, null, "Enable automatic renaming of recorded files");
			AutoRenameString = AutoRenameSettings.CreateEntry("Auto Rename String", "{date} {time} vs {player}", null, "Rename format for recorded files. Use {player}, {date}, and {time} as variables.");
			DateFormat = AutoRenameSettings.CreateEntry("Date Format", "yyyy-MM-dd", null, "Date format for renaming. https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
			TimeFormat = AutoRenameSettings.CreateEntry("Time Format", "HH-mm-ss", null, "Time format for renaming.");
			ReplayPrefix = AutoRenameSettings.CreateEntry("Replay Prefix", "R- ", null, "String to prefix replay buffers with.");
			

			RecordingSettings = MelonPreferences.CreateCategory("Recording Settings");
			RecordingSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			RecordingPauseHoldTimeout = RecordingSettings.CreateEntry("Recording Hold Timeout", 0, null, "Seconds to keep the recording held before stopping automatically");
			PauseAfterMatch = RecordingSettings.CreateEntry("Pause recording after match", false, null, "Pause recording on returning to gym. Replay buffer will not work when paused");
			RecordByBPThreshold = RecordingSettings.CreateEntry("BP Threshold", -1, "BP", "Record players with BP greater than value. -1 = disabled");
			AddChapterMarkers = RecordingSettings.CreateEntry("Chapter Markers", true, null, "Enabling will write chapter markers to the output video if the format supports it (currently only Hybrid MP4)");


			IndicatorSettings = MelonPreferences.CreateCategory("Indicator Settings");
			IndicatorSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			PreferMinimalIcon = IndicatorSettings.CreateEntry("Prefer Minimal Icon", false, null, "Prefer Minimal OBS Icon for Recording indicator (This is kinda broken)");
			ClippingIconVisibleByDefault = IndicatorSettings.CreateEntry("Clip Icon Default Visibility", true, null, "Make the replay buffer icon always visible. Otherwise, it's only shown to show an inactive replay buffer and blinks when a clip is saved");
			RockCamVisibility = IndicatorSettings.CreateEntry("Show Icons on Camera", true, null, "Make Icons Visible on Rock Cam and Legacy Cam");



			AutoRecordList = File.ReadAllLines(Path.Combine(USER_DATA, RECORD_LIST)).ToList();

			OBSAutoRecorderSettings.SaveToFile();

			foreach (string entry in AutoRecordList)
			{
				Log(entry, true);
			}
			Log($"Debugging Mode Is: {isDebugMode.Value}");

		}

		private void UpdateAutoRecordFile()
		{
			string fullAutoRecordPath = (Path.Combine(USER_DATA, RECORD_LIST));
			Log($"Writing people to file {RECORD_LIST}", true);
			if (!File.Exists(fullAutoRecordPath))
			{
				Log($"Missing {RECORD_LIST} file. Creating File", false, 1);
				File.Create(fullAutoRecordPath);
			}

			foreach (string person in AutoRecordList)
			{
				Log($"{person}", true);
			}
			File.WriteAllLines(fullAutoRecordPath, AutoRecordList);
		}


		public override void OnLateInitializeMelon()
		{
			Calls.onMapInitialized += OnMapInitialized;

			OBS.onRecordingPaused += onRecordPause;
			OBS.onRecordingStopped += onRecordStop;
			OBS.onRecordingStarted += onRecordStart;
			OBS.onRecordingResumed += onRecordResume;

			OBS.onConnect += onConnect;
			OBS.onReplayBufferSaved += onReplayBufferSaved;

			Calls.onPlayerSpawned += onPlayerSpawn;
			Instance = this;
		}
		public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
		{
			_sceneIsLoaded = false;
		}
		public override void OnUpdate()
		{
			
			if (!_sceneIsLoaded)
				return;

			SetIndicatorState();

			LogDiff($"Record: {OBS.IsRecordingActive()}, Pause: {IsPaused}, External Recording: {ExternalRecording}, FighterInMap: {(ActivePlayerInArena  is null ? "-" : ActivePlayerInArena.Name)}, LastRecorded: {(LastRecordedPlayer is null ? "-" : LastRecordedPlayer.Name)}" );

			if(DebugUiText != null)
			{
				try
				{
					DebugUiText.text = $"Record: {OBS.IsRecordingActive()}, Pause: {IsPaused}, External: {ExternalRecording}, \nFighterInMap: {(ActivePlayerInArena is null ? "-" : ActivePlayerInArena.Name)}, LastRecorded: {(LastRecordedPlayer is null ? "-" : LastRecordedPlayer.Name)}\nHold Coroutine: {!(_stopQueueCor is null)}";
				}
				catch(System.Exception ex)
				{
					Log(ex.Message, true);
				}
			}
		}
		/// <summary>
		/// Called when map is fully initialized reducing the risk of null references.
		/// </summary>
		private void OnMapInitialized()
		{




			//addButtonsToFriendsScreen();
			if (SceneName == "gym")
			{
				_scrollBar = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.FriendScreen.FriendScrollBar.GetGameObject();
				_selectedTag = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.SettingsScreen.PlayerTags.PlayerTag201.GetGameObject();
				TagFrame = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
				RecentTags = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.RecentScreen.PlayerTags.GetGameObject();

			}
			if (SceneName == "park")
			{
				_scrollBar = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.FriendScreen.FriendScrollBar.GetGameObject();
				_selectedTag = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.SettingsScreen.PlayerTags.PlayerTag201.GetGameObject();
				TagFrame = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
				RecentTags = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.RecentScreen.PlayerTags.GetGameObject();
			}

			if (SceneName != "loader")
			{
				
				if (isFirstLoad)
				{
					FirstLoad();
				}
				BuildPlayerIndicators();
			}

			if (SceneName == "gym" || SceneName == "park")
			{




				for (int i = 0; i < 4; i++)
				{
					_scrollBar.transform.GetChild(i).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
					{
						_previousList.Clear();
						_previousList = _displayedFriendTags.Select(x => x.ToString()).ToList();
						foreach (string entry in _previousList)
						{
							Log(entry, true);
						}
						if (_pollPageCor != null)
						{

							MelonCoroutines.Stop(_pollPageCor);
							_pollPageCor = null;
						}
						_pollPageCor = MelonCoroutines.Start(PollPageTurnCoRoutine());
					});
				}

				BuildTagHolders();

				isFirstLoad = false;
				
			}





			//Solo recording start test
			if (SceneName == "park")
			{
				//StartRecording(" Pre <#> <AEDF12> Invalid char test");
			}

			//Test code. Remove later
			/*else if (SceneName.Contains("map") && PlayerManager.instance.AllPlayers.Count > 1)
			{
				
			}*/

			SetRecordingState();
			_sceneIsLoaded = true;
			LastSceneName = SceneName;
		}



		/// <summary>
		/// Toggles the auto-record status for the selected tag. Adds or removes the friend from the auto-record list
		/// based on their current status.
		/// </summary>
		/// <remarks>Only run on the selected tag in the settings screen</remarks>
		/// <param name="selected">The friend whose auto-record status is to be toggled. Cannot be null. The friend's PlayFabID is used to identify
		/// them in the auto-record list.</param>
		private void ToggleAutoRecord(TagHolder selected)
		{

			if (IsInAutoRecordList(selected))
			{
				AutoRecordList.RemoveAll(x => x.Split(" - ")[0].Trim().ToLower() == selected.PlayFabID.Trim().ToLower());
				selected.AutoRecordable = false;
				Log($"Removed {selected.ToString()} from AutoRecord list", false);
			}
			else
			{
				AutoRecordList.Add($"{selected.PlayFabID} - {selected.PublicName}");
				selected.AutoRecordable = true;
				Log($"Added {selected.ToString()} to AutoRecord list", false);
			}

			//PlayersToRecord.Value = string.Join(SEPARATOR, AutoRecordList);
			UpdateAutoRecordFile();
			OBSAutoRecorderSettings.SaveToFile();

			foreach (TagHolder friend in _displayedFriendTags)
			{
				friend.AutoRecordable = IsInAutoRecordList(friend);
			}

		}



		private IEnumerator DebounceCoRoutine(TagHolder holder)
		{
			holder.WasPressed = true;
			yield return new WaitForSeconds(0.5f);
			holder.WasPressed = false;
			_debounceCor = null;
		}

		private IEnumerator PollPlayerTagsCoroutine()
		{
			_isPolling = true;
			float startTime = Time.realtimeSinceStartup;
			Log("Starting to poll for player tags...", true);


			while (!IsFriendInfoLoaded())
			{
				yield return null;
			}
			UpdateDisplayedTags();
			_selectedFriend.PlatformStatus.SetActive(false);


			_isPolling = false;
			_pollPageCor = null;
		}
		void UpdateDisplayedTags()
		{
			foreach (TagHolder info in _displayedFriendTags)
			{
				info.AutoRecordable = IsInAutoRecordList(info);
				//Log(info.PublicName, true);
			}
			
		}
		private IEnumerator PollPageTurnCoRoutine()
		{
			float start = Time.realtimeSinceStartup;


			while (/*SameTagsAsLast() &&*/ Time.realtimeSinceStartup - start < 1.5f)
			{
				UpdateDisplayedTags();
				yield return new WaitForSeconds(0.1f);
				Log("\n", true);
			}
			_pollPageCor = null;
		}

		private bool SameTagsAsLast()
		{
			for (int i = 0; i < _previousList.Count; i++)
			{
				bool match = _previousList[i] == _displayedFriendTags[i].ToString();
				Log($"{i} {match} {_previousList[i]} with {_displayedFriendTags[i].ToString()}", true);
				if (match)
				{
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Scans the player tags collection and updates the displayed friend tags list.
		/// </summary>
		/// <remarks></remarks>
		/// <returns>true if all player tags are found and processed successfully; otherwise, false.</returns>
		bool IsFriendInfoLoaded()
		{

			return _displayedFriendTags.TrueForAll(x => !(string.IsNullOrEmpty(x.PlayFabID)));
			

		}


		private bool IsInAutoRecordList(TagHolder friend)
		{
			return IsInAutoRecordList(friend.ToString());
		}

		private bool IsInAutoRecordList(string playFabID)
		{

			var targets = AutoRecordList.Where(x => x.Split(" - ")[0].Trim().ToLower() == playFabID.Split(" - ")[0].Trim().ToLower()).ToList();

			if (targets.Count > 1)
			{
				Log($"Warning: More than one entry found for {playFabID.Split(" - ")[0]} in AutoRecord list. {targets.Count}", false, 1);
			}

			foreach (string entry in targets)
			{
				Log($"Found target: {entry}", true);
			}
			
			bool result = targets.Count > 0;
			return result;
		}
		/// <summary>
		/// Logs a message to the console
		/// </summary>
		/// <param name="message"></param>
		/// <param name="debugOnly"></param>
		/// <param name="logLevel">0 = normal, 1 = warning, 2 = error</param>
		public void Log(string message, bool debugOnly = false, int logLevel = 0)
		{
			if (debugOnly && !isDebugMode.Value)
				return;

			switch (logLevel)
			{
				case 1:
					LoggerInstance.Warning("Warn: " + message);
					break;
				case 2:
					LoggerInstance.Error("Error: " + message);
					break;
				default:
					LoggerInstance.Msg(message);
					break;
			}
		}

		private void LogDiff(string message, int logLevel = 0)
		{
			if(message != lastLogDiff)
			{
				Log($"##LOGDIFF: {message}", true, logLevel);
				lastLogDiff = message;
			}

		}



		private IEnumerator StartRecordingAfterStopCoroutine()
		{
			float startTime = Time.realtimeSinceStartup;
			float currentTime = Time.realtimeSinceStartup - startTime;
			while ((OBS.IsRecordingActive() || IsPaused) && currentTime < 5f)
			{
				yield return null;
				currentTime = Time.realtimeSinceStartup - startTime;
				Log($"Waiting for last recording end", true);
			}

			if (currentTime >= 5f && OBS.IsRecordingActive())
			{
				Log($"Restart recording for new player failed: timeout", false, 1);
			}
			else
			{
				Log($"Last recording stopped. Starting new recording for {LastRecordedPlayer.ToString()}");
				//StartRecording(NextPlayerToRecord);
			}
			//_recordingWaitCor = null;
		}

		
		

	}
}
