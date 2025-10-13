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
		//private MelonPreferences_Entry<string> PlayersToRecord;
		private MelonPreferences_Entry<string> AutoRenameString;
		private MelonPreferences_Entry<bool> DoAutoRename;
		private MelonPreferences_Entry<string> DateFormat;
		private MelonPreferences_Entry<string> TimeFormat;
		private MelonPreferences_Entry<string> ReplayPrefix;
		private MelonPreferences_Entry<bool> AddChapterMarkers;
		private MelonPreferences_Entry<int> RecordingPauseHoldTimeout;
		private MelonPreferences_Entry<bool> PreferMinimalIcon;
		private MelonPreferences_Entry<int> RecordByBPThreshold;
		private MelonPreferences_Entry<bool> PauseAfterMatch;
		private List<string> AutoRecordList { get; set; } = new();



		private static GameObject IndicatorsBase;
		RequestResponse.GetRecordStatus getRecordStatus = new();



		private Color pauseColor = new Color(1f, 1f, 0f, 0.75f);
		private Color recordColor = new Color(1f, 1f, 1f, 0.75f);

		private object _debounceCor = null;
		private object _pollTagsCor = null;
		private object _pollPageCor = null;
		private object _stopQueueCor = null;
		private object _recordingWaitCor = null;
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
			if (ModInitiatedRecording)
			{
				Log("Game Closing. Forcing recording stop");
				StopRecording();
				OBS.StopRecord();
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

			isDebugMode = OBSAutoRecorderSettings.CreateEntry("Debug Mode", true, null, "Enable debug with more verbose logging");

			RecordByBPThreshold = OBSAutoRecorderSettings.CreateEntry("BP Threshold", -1, "BP", "Record players with BP greater than value. -1 = disabled");

			DoAutoRename = OBSAutoRecorderSettings.CreateEntry("Enable Auto Rename", true, null, "Enable automatic renaming of recorded files");
			AutoRenameString = OBSAutoRecorderSettings.CreateEntry("Auto Rename String", "{date} {time} vs {player}", null, "Rename format for recorded files. Use {player}, {date}, and {time} as variables.");
			DateFormat = OBSAutoRecorderSettings.CreateEntry("Date Format", "yyyy-MM-dd", null, "Date format for renaming. https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
			TimeFormat = OBSAutoRecorderSettings.CreateEntry("Time Format", "HH-mm-ss", null, "Time format for renaming.");
			ReplayPrefix = OBSAutoRecorderSettings.CreateEntry("Replay Prefix", "R- ", null, "String to prefix replay buffers with.");
			AddChapterMarkers = OBSAutoRecorderSettings.CreateEntry("Chapter Markers", false, null, "Enabling will write chapter markers to the output video if the format supports it (currently only Hybrid MP4)");

			RecordingPauseHoldTimeout = OBSAutoRecorderSettings.CreateEntry("Recording Hold Timeout", 180, null, "Seconds to keep the recording paused until auto stop");
			PauseAfterMatch = OBSAutoRecorderSettings.CreateEntry("Pause recording after match", false, null, "Pause recording when not fighting recordable player. Replay buffer will not work when paused");

			PreferMinimalIcon = OBSAutoRecorderSettings.CreateEntry("Prefer Minimal Icon", false, null, "Prefer Minimal OBS Icon for Recording indicator");
			//PlayersToRecord = OBSAutoRecorderSettings.CreateEntry("PlayersToRecord", "", "List of players to Record");
			//AutoRecordList = PlayersToRecord.Value.Split(SEPARATOR).ToList();



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


			//Log($"OBS.IsRecordingActive(): {OBS.IsRecordingActive()}\tIsPaused: {IsPaused}", true);
			if (PreferMinimalIcon.Value)
			{
				try
				{
					MinimalLogo.SetActive(OBS.IsRecordingActive() || IsPaused);
					MinimalLogo.GetComponent<MeshRenderer>().material.color = IsPaused ? pauseColor : recordColor;
				}
				catch (System.Exception ex)
				{
					Log($"ObsAutoRecorder: {ex.Message}", false, 2);

				}


			}
			else
			{
				try
				{
					OBSIcon.SetActive(IsPaused || OBS.IsRecordingActive());
					PauseIcon.SetActive(IsPaused);
					//&&!IsPaused required due to inconsistency in OBS API. 
					RecordIcon.SetActive(OBS.IsRecordingActive());

				}
				catch (System.Exception ex)
				{
					Log($"OBS Control API error: {ex.Message}", false, 2);
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

			}
			if (SceneName == "park")
			{
				_scrollBar = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.FriendScreen.FriendScrollBar.GetGameObject();
				_selectedTag = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.SettingsScreen.PlayerTags.PlayerTag201.GetGameObject();
				TagFrame = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
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
				Log($"Warning: More than one entry found for {playFabID.Split(" - ")[0]} in AutoRecord list", false, 1);
			}

			foreach (string entry in targets)
			{
				Log($"Found target: {entry}", true);
			}
			Log($"{targets.Count()}", true);

			bool result = targets.Count > 0;
			Log($"Checking {playFabID} if in auto record list: {result}", true);
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
				Log($"Last recording stopped. Starting new recording for {CurrentRecordedPlayer.ToString()}");
				StartRecording(NextPlayerToRecord);
			}
			_recordingWaitCor = null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="duration"></param>
		/// <returns></returns>
		/// <remarks>TODO: Ensure that user-started recordings are not stopped</remarks>
		private IEnumerator RecordingHoldCoroutine(float duration)
		{
			Log($"Coroutine started: Stop Queue States: QueuedForStopping: {QueuedForStopping}, ModInitiatedRecording {ModInitiatedRecording}", true);
			yield return new WaitForSeconds(duration);
			Log($"Stop Queue States: QueuedForStopping: {QueuedForStopping}, ModInitiatedRecording {ModInitiatedRecording}", true);
			if (QueuedForStopping && ModInitiatedRecording)
			{
				StopRecording();
			}
			_stopQueueCor = null;
		}
		

	}
}
