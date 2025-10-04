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
	public class TagHolder : MelonMod
	{
		//ideal location for autorecord status 0.2391 -0.0336 -0.0091
		//friendblock path --------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Friend Screen/Player Tags/Player Tag 2.0/InteractionButton/Meshes/
		//status block location: playertag 0 0 0 
		public bool WasPressed { get; set; } = false;

		public GameObject RecordIconBlock { get; private set; }
		public GameObject RecordIcon { get; private set; }


		private bool _autoRecordable = false;

		public GameObject InteractionButton
		{
			get
			{
				return _tagObject.transform.GetChild(0).gameObject;
			}
		}
		/// <summary>
		/// Gets or sets a value indicating whether automatic recording is enabled.
		/// </summary>
		/// <remarks>When set to <see langword="true"/>, the UI updates to reflect the auto-recording status. Changing
		/// this property may affect the appearance of the record icon.</remarks>
		public bool AutoRecordable
		{
			get { return _autoRecordable; }
			set
			{
				_autoRecordable = value;
				Color statusColor = _autoRecordable ? new Color(0.45f, 0.31f, 0.22f, 1f) : new Color(0.56f, 0.52f, 0.4f, 1f);
				RecordIcon.transform.GetChild(0).GetComponent<RawImage>().color = statusColor;
			}
		}
		public string PlayFabID
		{
			get
			{
				return _tagObject.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.playFabMasterId;
			}
		}
		public string PublicName
		{
			get
			{
				return Sanitize(_tagObject.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.publicName);
			}
		}
		/*public GameObject StatusIcon
		{
			get
			{
				return _tagObject.transform.GetChild(0).GetChild(1).GetChild(3).GetChild(0).gameObject;
			}
		}*/
		private GameObject _tagObject;

		/// <summary>
		/// Returns a string that represents the current object, including the PlayFab ID and public name.
		/// </summary>
		/// <returns>A string in the format "PlayFabID - PublicName" representing the current object.</returns>
		public override string ToString()
		{
			return $"{PlayFabID} - {PublicName}";
		}

		public GameObject TagObject
		{
			get { return _tagObject; }
			set
			{
				_tagObject = value;
				ObsAutoRecorder.Instance.Log($"TagObject set for {PublicName}", true);
				CreateAutoRecordBlock();
			}
		}

		private void CreateAutoRecordBlock()
		{
			RecordIconBlock = GameObject.Instantiate(TagObject.transform.GetChild(0).GetChild(0).GetChild(0).gameObject);
			RecordIconBlock.transform.SetParent(TagObject.transform.GetChild(0).GetChild(0), false);
			RecordIconBlock.transform.localPosition = new Vector3(0.2391f, -0.0336f, -0.0091f);

			RecordIcon = ObsAutoRecorder.GetIndicator();
			RecordIcon.transform.SetParent(RecordIconBlock.transform, false);
			RecordIcon.SetActive(true);
			RecordIcon.transform.localPosition = new Vector3(0, 0.5f, 0);
			//0.0085 0.0085 0.0085
			RecordIcon.transform.localScale = new Vector3(0.0085f, 0.0085f, 0.0085f);
			RecordIcon.transform.localRotation = Quaternion.Euler(90, 0, 0);
			//new Color (R = .45, G = .31, B = .22)
			AutoRecordable = false;

		}

		public TagHolder()
		{


		}

		static string Sanitize(string Input)
		{

			string pattern = @"<[^>]*>";
			return Regex.Replace(Input, pattern, string.Empty);
		}
	}
	public class ObsAutoRecorder : MelonMod
	{
		//Hold button location 
		//--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Settings Screen/InteractionButton (1)/
		private const string USER_DATA = "UserData/ObsAutoRecorder/";
		private const string CONFIG_FILE = "config.cfg";
		private const string SEPARATOR = "\n";
		public static ObsAutoRecorder Instance { get; private set; }

		string SceneName { get; set; }
		private MelonPreferences_Category OBSAutoRecorderSettings;
		private MelonPreferences_Entry<bool> isDebugMode;
		private MelonPreferences_Entry<string> PlayersToRecord;
		private MelonPreferences_Entry<string> AutoRenameString;
		private MelonPreferences_Entry<bool> DoAutoRename;
		private MelonPreferences_Entry<string> DateFormat;
		private MelonPreferences_Entry<string> TimeFormat;
		private MelonPreferences_Entry<int> RecordingPauseHoldTimeout;
		private MelonPreferences_Entry<bool> PreferMinimalIcon;
		private List<string> AutoRecordList = new();

		bool isFirstLoad = true;
		private bool _isPolling = false;
		bool _sceneIsLoaded = false;

		private GameObject TagFrame;
		private List<TagHolder> _displayedFriendTags = new();
		private GameObject HoldButton;
		private List<GameObject> HoldButtons = new();
		private GameObject LogoPack { get; set; }
		private GameObject PauseIcon { get; set; }
		private GameObject RecordIcon { get; set; }
		private GameObject OBSIcon { get; set; }

		private GameObject _scrollBar;
		private GameObject PlayerUi;
		private GameObject _recordingIndicatorBase;
		private GameObject _recordingIndicator;



		private List<string> _previousList = new();
		private GameObject _selectedTag = new();
		private TagHolder _selectedFriend;

		private static GameObject IndicatorsBase;
		RequestResponse.GetRecordStatus getRecordStatus = new();

		//OBS Recording states
		private string CurrentOrLastRecordedPlayer { get; set; } = "";
		private bool IsRecording { get; set; } = false;
		private bool IsPaused { get; set; } = false;
		private bool ModInitiatedRecording { get; set; } = false;
		private bool ModInitiatedPause { get; set; } = false;
		private bool QueuedForStopping { get; set; } = false;
		private bool ModInitiatedStop { get; set; } = false;


		private bool StartRequestedByMod = false;
		private bool StopRequestedByMod = false;
		private bool PauseRequestedByMod = false;


		private Color pauseColor = new Color(1f, 1f, 0f, 0.75f);
		private Color recordColor = new Color(1f, 1f, 1f, 0.75f);

		private object _debounceCor;
		private object _pollTagsCor;
		private object _pollPageCor;
		private object _stopQueueCor;
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
			OBSAutoRecorderSettings.SaveToFile();
		}
		public override void OnInitializeMelon()
		{
			OBSAutoRecorderSettings = MelonPreferences.CreateCategory("ObsAutoRecorder");
			OBSAutoRecorderSettings.SetFilePath(@"UserData/ObsAutoRecorder.cfg");

			isDebugMode = OBSAutoRecorderSettings.CreateEntry("isDebugMode", false, "Enable debug logging");
			AutoRenameString = OBSAutoRecorderSettings.CreateEntry("AutoRenameString", "{date} {time} vs {player}", "Rename format for recorded files. Use {player}, {date}, and {time} as variables.");
			DoAutoRename = OBSAutoRecorderSettings.CreateEntry("DoAutoRename", false, "Enable automatic renaming of recorded files");
			DateFormat = OBSAutoRecorderSettings.CreateEntry("DateFormat", "yyyy-MM-dd", "Date format for renaming. Uses standard C# date formatting.");
			TimeFormat = OBSAutoRecorderSettings.CreateEntry("TimeFormat", "HH-mm-ss", "Time format for renaming. Uses standard C# time formatting.");
			RecordingPauseHoldTimeout = OBSAutoRecorderSettings.CreateEntry("PauseHoldTimeout", 180, "Seconds to keep the recording paused until auto stop");
			PreferMinimalIcon = OBSAutoRecorderSettings.CreateEntry("PreferMinimalIcon", false, "Prefer Minimal OBS Icon for Recording indicator");
			PlayersToRecord = OBSAutoRecorderSettings.CreateEntry("PlayersToRecord", "", "List of players to Record");
			AutoRecordList = PlayersToRecord.Value.Split(SEPARATOR).ToList();


			OBSAutoRecorderSettings.SaveToFile();

			foreach (string entry in AutoRecordList)
			{
				Log(entry, true);
			}
		}
		public override void OnLateInitializeMelon()
		{
			Calls.onMapInitialized += OnMapInitialized;

			OBS.onRecordingPaused += onRecordPause;
			OBS.onRecordingStopped += onRecordStop;
			OBS.onRecordingStarted += onRecordStart;
			OBS.onRecordingResumed += onRecordResume;


			Instance = this;
		}
		public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
		{
			_sceneIsLoaded = false;
		}
		/// <summary>
		/// Called when map is fully initialized reducing the risk of null references.
		/// </summary>
		private void OnMapInitialized()
		{

			Log(SceneName, true);


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
			Log("Starting poll for player tags...", true);
			if (SceneName == "gym" || SceneName == "park")
			{
				if (isFirstLoad)
				{
					LogoPack = GameObject.Instantiate(Calls.LoadAssetFromStream<GameObject>(this, "ObsAutoRecorder.Assets.obsasset", "logopack"));
					GameObject.DontDestroyOnLoad(LogoPack);
					LogoPack.SetActive(false);


					IndicatorsBase = LogoPack.transform.GetChild(1).gameObject;
					IndicatorsBase.SetName("OBS Logo");
					GameObject.DontDestroyOnLoad(IndicatorsBase);
					//_recordingIndicatorBase = GameObject.Instantiate(IndicatorsBase);
					//_recordingIndicatorBase.SetActive(false);
					IndicatorsBase.transform.GetChild(0).GetComponent<RawImage>().color = Color.black;

					IndicatorsBase.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

					IndicatorsBase.SetActive(false);
				}


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


				_selectedFriend = new TagHolder() { TagObject = _selectedTag };
				_displayedFriendTags = GetPlayerTags();
				if (_pollTagsCor != null)
				{
					MelonCoroutines.Stop(_pollTagsCor);
					_pollTagsCor = null;
				}
				_pollTagsCor = MelonCoroutines.Start(PollPlayerTagsCoroutine());


				_selectedFriend.InteractionButton.GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
				{
					if (_selectedFriend.WasPressed)
						return;

					MelonCoroutines.Start(DebounceCoRoutine(_selectedFriend));
					ToggleAutoRecord(_selectedFriend);
				});

				isFirstLoad = false;
			}

			_recordingIndicator = GameObject.Instantiate(IndicatorsBase);
			//GameObject.DontDestroyOnLoad(_recordingIndicator);
			_recordingIndicator.SetName("RecordingIndicator");

			PlayerUi = PlayerManager.Instance.LocalPlayer.Controller.gameObject.transform.GetChild(6).GetChild(0).gameObject;
			_recordingIndicator.transform.SetParent(PlayerUi.transform, false);
			_recordingIndicator.transform.localScale = new Vector3(0.0003f, 0.0003f, 0.0003f);
			_recordingIndicator.transform.localPosition = new Vector3(-0.22f, 0.05f, 0.95f);

			_recordingIndicator.transform.localRotation = Quaternion.Euler(20, 335, 0);
			_recordingIndicator.transform.GetChild(0).GetComponent<RawImage>().color = new Color(1f, 1f, 1f, 0.75f);

			OBSIcon = GameObject.Instantiate(LogoPack.transform.GetChild(0).gameObject);
			PauseIcon = OBSIcon.transform.GetChild(0).gameObject;
			PauseIcon.transform.localPosition = new Vector3(0.4f, -5f, -0.4f);
			PauseIcon.transform.localRotation = Quaternion.Euler(270, 0, 0);
			RecordIcon = OBSIcon.transform.GetChild(1).gameObject;
			RecordIcon.transform.localPosition = new Vector3(0.4f, -5f, -0.4f);
			OBSIcon.transform.SetParent(PlayerUi.transform);
			//-0.24 0.035 0.945
			OBSIcon.transform.localPosition = new Vector3(-0.24f, 0.035f, 0.945f);

			//70.0001 155 180
			OBSIcon.transform.localRotation = Quaternion.Euler(70, 155, 180);
			OBSIcon.transform.localScale = new Vector3(0.03f, 0.0001f, 0.03f);
			OBSIcon.SetActive(false);



			//Solo recording start test
			if (SceneName == "park")
			{
				//StartRecording("solo test");
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

			PlayersToRecord.Value = string.Join(SEPARATOR, AutoRecordList);
			OBSAutoRecorderSettings.SaveToFile();

			foreach (TagHolder friend in _displayedFriendTags)
			{
				friend.AutoRecordable = IsInAutoRecordList(friend);
			}

		}

		private List<TagHolder> GetPlayerTags()
		{
			List<TagHolder> friendInfos = new();

			for (int i = 0; i < TagFrame.transform.childCount; i++)
			{
				TagHolder friendInfo = new TagHolder();
				friendInfo.TagObject = TagFrame.transform.GetChild(i).gameObject;
				friendInfos.Add(friendInfo);
				friendInfo.InteractionButton.GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
				{
					_selectedFriend.AutoRecordable = IsInAutoRecordList(friendInfo);
				});
			}
			return friendInfos;

		}

		IEnumerator DebounceCoRoutine(TagHolder holder)
		{
			holder.WasPressed = true;
			yield return new WaitForSeconds(0.5f);
			holder.WasPressed = false;
			_debounceCor = null;
		}

		IEnumerator PollPlayerTagsCoroutine()
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
		IEnumerator PollPageTurnCoRoutine()
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

		public override void OnFixedUpdate()
		{
			if (!_sceneIsLoaded)
				return;


			if (!isFirstLoad)
			{
				if (PreferMinimalIcon.Value)
				{
					try
					{
						_recordingIndicator.transform.GetChild(0).GetComponent<RawImage>().color = IsPaused ? pauseColor : recordColor;
						//IsRecording = OBS.IsRecordingActive();
						_recordingIndicator.SetActive(OBS.IsRecordingActive() || IsPaused);
					}
					catch (System.Exception ex)
					{
						Log($"OBS Control API error: {ex.Message}", false, 2);

					}
				}
				else
				{
					try
					{
						OBSIcon.SetActive(IsPaused || OBS.IsRecordingActive());
						PauseIcon.SetActive(IsPaused);
						RecordIcon.SetActive(OBS.IsRecordingActive());

					}
					catch (System.Exception ex)
					{
						Log($"OBS Control API error: {ex.Message}", false, 2);
					}
				}

			}
		}
		private bool IsInAutoRecordList(TagHolder friend)
		{
			/*var targets = AutoRecordList.Where(x => x.Split('-')[0].Trim().ToLower() == friend.PlayFabID.Trim().ToLower()).ToList();
			if (targets.Count > 1)
			{
				Log($"Warning: More than one entry found for {friend.PlayFabID} in AutoRecord list", false, 1);
			}*/
			return IsInAutoRecordList(friend.PlayFabID);
		}

		private bool IsInAutoRecordList(string playFabID)
		{
			Log($"Checking {playFabID} if autorecordable");
			var targets = AutoRecordList.Where(x => x.Split(" - ")[0].Trim().ToLower() == playFabID.Split(" - ")[0].Trim().ToLower()).ToList();

			if (targets.Count > 1)
			{
				Log($"Warning: More than one entry found for {playFabID} in AutoRecord list", false, 1);
			}

			foreach (string entry in targets)
			{
				Log($"Found target: {entry}", true);
			}
			Log(targets.Count().ToString(), true);

			return targets.Count > 0;
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
					LoggerInstance.Warning(message);
					break;
				case 2:
					LoggerInstance.Error(message);
					break;
				default:
					LoggerInstance.Msg(message);
					break;
			}
		}


		private void SetRecordingState()
		{
			if (!OBS.IsConnected())
			{
				Log("No active websocket connection to OBS detected", false, 1);
				return;
			}

			if (SceneName == "gym")
			{
				WhenInGym();
			}


			if (SceneName.Contains("map") && PlayerManager.instance.AllPlayers.Count > 1)
			{
				WhenInArena();
			}
		}
		private void WhenInGym()
		{
			if (OBS.IsRecordingActive())
			{
				if (ModInitiatedRecording)
				{
					if (!QueuedForStopping && !IsPaused)
					{

						PauseRecording();
						QueuedForStopping = true;
						if (_stopQueueCor != null)
						{
							MelonCoroutines.Stop(_stopQueueCor);
							_stopQueueCor = null;
						}
						Log("Starting Stop Hold Coroutine");
						_stopQueueCor = MelonCoroutines.Start(RecordingHoldCoroutine(RecordingPauseHoldTimeout.Value));
					}
				}
			}
		}
		private void WhenInArena()
		{
			var opp = PlayerManager.instance.AllPlayers[1];
			var oppId = opp?.Data?.GeneralData?.PlayFabMasterId;
			var oppName = opp?.Data?.GeneralData?.PublicUsername ?? "Unknown";

			string opponentInfo = $"{oppId} - {oppName}";
			if (IsPaused)
			{

				if (ModInitiatedPause && (CurrentOrLastRecordedPlayer == opponentInfo))
				{
					ResumeRecording();
				}
				else
				{
					if (ModInitiatedPause)
					{

						if (IsInAutoRecordList(opponentInfo))
						{
							StopRecording();
							Log($"Recording started through onMapInitialized", true);
							StartRecording(opponentInfo);
						}
					}
				}
			}
			else
			{
				if (IsInAutoRecordList(opponentInfo))
				{
					Log($"Recording started through onMapInitialized", true);
					StartRecording(opponentInfo);
				}
			}
		}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="duration"></param>
		/// <returns></returns>
		/// <remarks>TODO: Ensure that user-started recordings are not stopped</remarks>
		private IEnumerator RecordingHoldCoroutine(float duration)
		{
			Log($"Coroutine started: Stop Queue States: QueuedForStopping: {QueuedForStopping}, ModInitiatedRecording {ModInitiatedRecording}");
			yield return new WaitForSeconds(duration);
			Log($"Stop Queue States: QueuedForStopping: {QueuedForStopping}, ModInitiatedRecording {ModInitiatedRecording}");
			if (QueuedForStopping && ModInitiatedRecording)
			{
				StopRecording();
			}
			_stopQueueCor = null;
		}
		/// <summary>
		/// Start recording session
		/// </summary>
		/// <param name="playerID">ID - PublicName of other player loaded in the park</param>
		/// <remarks>Sets ModInitiatedRecording to true</remarks>
		private void StartRecording(string playerID = "")
		{

			if (OBS.IsRecordingActive() || IsPaused)
			{
				string pauseStatus = IsPaused ? "Paused " : "";
				Log($"{pauseStatus} Recording already in progress", false);

				return;
			}
			StartRequestedByMod = true;
			Log($"Starting recording for: {playerID}", false);
			CurrentOrLastRecordedPlayer = playerID;
			OBS.StartRecord();
		}

		private void StopRecording()
		{
			if (!(OBS.IsRecordingActive() || IsPaused))
			{
				Log("No recording in progress", false);
				return;
			}
			StopRequestedByMod = true;
			QueuedForStopping = false;

			OBS.StopRecord();

		}
		private void PauseRecording()
		{

			if (!(OBS.IsRecordingActive() || IsPaused))
			{
				Log("No recording in progress", true);
				return;
			}
			if (IsPaused)
				return;
			PauseRequestedByMod = true;
			OBS.PauseRecord();
		}

		private void ResumeRecording()
		{
			OBS.ResumeRecord();
			ModInitiatedPause = false;
			QueuedForStopping = false;
		}


		private void onRecordPause()
		{
			IsPaused = true;
			ModInitiatedPause = PauseRequestedByMod;
			PauseRequestedByMod = false;
			Log($"Recording paused for: {CurrentOrLastRecordedPlayer}");
		}
		private void onRecordResume()
		{
			ModInitiatedRecording = true;
			IsPaused = false;
			QueuedForStopping = false;
			Log($"Recording Resumed for: {CurrentOrLastRecordedPlayer}");
		}
		private void onRecordStart(string outputPath)
		{
			IsPaused = false;
			//IsRecording = true;
			ModInitiatedRecording = StartRequestedByMod;
			StartRequestedByMod = false;
			Log($"Recording started for: {outputPath}");
		}
		private void onRecordStop(string outputPath)
		{
			ModInitiatedStop = StopRequestedByMod;
			StopRequestedByMod = false;
			if (!ModInitiatedStop)
				Log("Recording stopped Externally", false, 1);

			//File renaming
			if (DoAutoRename.Value && !string.IsNullOrEmpty(CurrentOrLastRecordedPlayer))
			{
				string playerName = "Unknown";
				if (!string.IsNullOrEmpty(CurrentOrLastRecordedPlayer))
				{
					playerName = CurrentOrLastRecordedPlayer.Split(" - ").Last().Trim();
				}
				else
				{
					playerName = "Unknown";
				}
				string date = System.DateTime.Now.ToString(DateFormat.Value);
				string time = System.DateTime.Now.ToString(TimeFormat.Value);
				string newFileName = AutoRenameString.Value.Replace("{player}", $"{playerName}").Replace("{date}", date).Replace("{time}", time);
				string newPath = System.IO.Path.GetDirectoryName(outputPath) + "/" + newFileName + System.IO.Path.GetExtension(outputPath);
				int copyIndex = 1;
				while (System.IO.File.Exists(newPath))
				{
					newPath = System.IO.Path.GetDirectoryName(outputPath) + "/" + newFileName + $" ({copyIndex})" + System.IO.Path.GetExtension(outputPath);
					copyIndex++;
				}
				try
				{
					System.IO.File.Move(outputPath, newPath);
					outputPath = newPath;
					Log($"Recording renamed to: {newFileName}", false);
				}
				catch (System.Exception ex)
				{
					Log($"Error renaming file: {ex.Message}", false, 2);
				}
			}
			Log($"Recording saved to: {outputPath}");

			//Reset recording states

			//IsRecording = false;
			IsPaused = false;
			ModInitiatedRecording = false;
			ModInitiatedPause = false;
			QueuedForStopping = false;
			ModInitiatedStop = false;
			CurrentOrLastRecordedPlayer = "";

		}
	}
}
