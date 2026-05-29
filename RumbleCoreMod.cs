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
using RumbleModdingAPI.RMAPI;

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
using UIFramework;
using System.Diagnostics;


[assembly: MelonInfo(typeof(ObsAutoRecorder.ObsAutoRecorder), ObsAutoRecorder.BuildInfo.Name, ObsAutoRecorder.BuildInfo.Version, ObsAutoRecorder.BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]
[assembly: MelonAuthorColor(255, 87, 166, 80)]
[assembly: MelonColor(255, 87, 166, 80)]
[assembly: MelonAdditionalDependencies("UIFramework")]

namespace ObsAutoRecorder
{
	public static class BuildInfo
	{
		public const string Name = "ObsAutoRecorder";
		public const string Author = "iListen2Sound";
		public const string Version = "1.3.1";
	}
	public partial class ObsAutoRecorder : MelonMod
	{
		private string lastLogDiff;

		//Hold button location 
		//--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Settings Screen/InteractionButton (1)/
		private const string USER_DATA = "UserData/ObsAutoRecorder/";
		private const string RECORD_LIST = "AutoRecordList.txt";
		private const string SEPARATOR = "\n";
		public static ObsAutoRecorder Instance { get; private set; }

		string SceneName { get; set; }

		private List<string> AutoRecordList { get; set; } = new();

		private static GameObject IndicatorsBase;

		public static GameObject DDOLParent;

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
			Log("SceneLoaded: " + sceneName, true, 1);
			MelonCoroutines.Start(DelayPlayerRetrieval());
		}

		private IEnumerator DelayPlayerRetrieval()
		{
			float defaultWaitTime = 0.01f;
			int attempts = 0;
			Stopwatch timeLimiter = Stopwatch.StartNew();
			do
			{
				try
				{
					PlayerUi = PlayerManager.Instance.LocalPlayer.Controller.gameObject.transform.GetChild(4).GetChild(0).gameObject;
				}
				catch (System.Exception)
				{
					PlayerUi = null;
				}
				if (PlayerUi is null)
				{
					Log("Player UI not found. Retrying...", true, 1);
					attempts++;
					yield return new WaitForSeconds(defaultWaitTime * attempts * 2);
				}


			} while (PlayerUi is null && timeLimiter.ElapsedMilliseconds < 10000);
			if (timeLimiter.ElapsedMilliseconds >= 10000)
			{
				Log("Failed to retrieve Player UI after multiple attempts. Aborting initialization to prevent errors.", false, 2);
				yield break;
			}
			timeLimiter.Stop();
			PlayerUIFound(SceneName);
			yield break;
		}

		public override void OnApplicationQuit()
		{
			if (!ExternalRecording)
			{
				Log("Game Closing. Forcing recording stop");
				RequestRecordingStop();
			}
			FindDeprecatedConfs();
		}

		public override void OnInitializeMelon()
		{	
			

			if (!Directory.Exists(USER_DATA))
				Directory.CreateDirectory(USER_DATA);

			if (!File.Exists(Path.Combine(USER_DATA, RECORD_LIST)))
				File.Create(Path.Combine(USER_DATA, RECORD_LIST));


			InitPreferences();
			UI.RegisterMelon(this, OBSAutoRecorderSettings, AutoRenameSettings, RecordingSettings, IndicatorSettings);

			AutoRecordList = File.ReadAllLines(Path.Combine(USER_DATA, RECORD_LIST)).ToList();

			SaveSettings();
			FindDeprecatedConfs();

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

		public override void OnDeinitializeMelon()
		{
		}
		

		public override void OnLateInitializeMelon()
		{


			//Actions.onMapInitialized += PlayerUIFound;

			OBS.onRecordingPaused += onRecordPause;
			OBS.onRecordingStopped += onRecordStop;
			OBS.onRecordingStarted += onRecordStart;
			OBS.onRecordingResumed += onRecordResume;

			OBS.onConnect += onConnect;
			OBS.onDisconnect += onDisconnect;
			OBS.onReplayBufferSaved += onReplayBufferSaved;

			Actions.onPlayerSpawned += onPlayerSpawn;
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

			LogDiff($"Record: {OBS.IsRecordingActive()}, Pause: {IsPaused}, External Recording: {ExternalRecording}, FighterInMap: {(ActivePlayerInArena is null ? "-" : ActivePlayerInArena.Name)}, LastRecorded: {(LastRecordedPlayer is null ? "-" : LastRecordedPlayer.Name)}");

			if (isDebugMode.Value && DebugUiText != null)
			{
				try
				{
					DebugUiText.text = $"TempTimeDir: {TempFileDir}\nTimeFileName: {TimeFileName} \nRecord: {OBS.IsRecordingActive()}, Pause: {IsPaused}, External: {ExternalRecording}, \nFighterInMap: {(ActivePlayerInArena is null ? "-" : ActivePlayerInArena.Name)}, LastRecorded: {(LastRecordedPlayer is null ? "-" : LastRecordedPlayer.Name)}\nHold Coroutine: {!(_stopQueueCor is null)}";
				}
				catch (System.Exception ex)
				{
					Log(ex.Message, true);
				}
			}
		}
		/// <summary>
		/// Called when map is fully initialized reducing the risk of null references.
		/// </summary>
		private void PlayerUIFound(string map)
		{
			ReadSettings();

			//SceneName = map.ToLower().Trim();
			ReadSettings();




			//addButtonsToFriendsScreen();
			if (SceneName == "gym")
			{
				_scrollBar = RumbleModdingAPI.RMAPI.GameObjects.Gym.INTERACTABLES.Telephone20REDUXspecialedition.FriendScreen.FriendScrollBar.GetGameObject();
				_selectedTag = RumbleModdingAPI.RMAPI.GameObjects.Gym.INTERACTABLES.Telephone20REDUXspecialedition.SettingsScreen.PlayerTags.PlayerTag201.GetGameObject();
				TagFrame = RumbleModdingAPI.RMAPI.GameObjects.Gym.INTERACTABLES.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
				RecentTags = RumbleModdingAPI.RMAPI.GameObjects.Gym.INTERACTABLES.Telephone20REDUXspecialedition.RecentScreen.PlayerTags.GetGameObject();

			}
			if (SceneName == "park")
			{
				_scrollBar = RumbleModdingAPI.RMAPI.GameObjects.Park.INTERACTABLES.Telephone20REDUXspecialedition.FriendScreen.FriendScrollBar.GetGameObject();
				_selectedTag = RumbleModdingAPI.RMAPI.GameObjects.Park.INTERACTABLES.Telephone20REDUXspecialedition.SettingsScreen.PlayerTags.PlayerTag201.GetGameObject();
				TagFrame = RumbleModdingAPI.RMAPI.GameObjects.Park.INTERACTABLES.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
				RecentTags = RumbleModdingAPI.RMAPI.GameObjects.Park.INTERACTABLES.Telephone20REDUXspecialedition.RecentScreen.PlayerTags.GetGameObject();
			}

			if (SceneName != "loader")
			{

				if (isFirstLoad)
				{
					DDOLParent = new GameObject("ObsAutoRecorder_DDOLParent");
					GameObject.DontDestroyOnLoad(DDOLParent);
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
			foreach (TagHolder info in _recentlyMetTags)
			{
				info.AutoRecordable = IsInAutoRecordList(info);

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
			if (message != lastLogDiff)
			{
				Log($"##LOGDIFF: {message}", true, logLevel);
				lastLogDiff = message;
			}

		}




	}
}
