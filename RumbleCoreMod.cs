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
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Video;


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
				return _tagObject.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.publicName;
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
	}
	public class ObsAutoRecorder : MelonMod
	{
		//Hold button location 
		//--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Settings Screen/InteractionButton (1)/
		private const string USER_DATA = "UserData/ObsAutoRecorder/";
		private const string CONFIG_FILE = "config.cfg";
		private const string SEPARATOR = "\n";
		public static ObsAutoRecorder Instance { get; private set; }

		string _sceneName;
		private MelonPreferences_Category OBSAutoRecorderSettings;
		private MelonPreferences_Entry<bool> isDebugMode;
		private MelonPreferences_Entry<string> PlayersToRecord;
		private List<string> AutoRecordList = new();

		bool isFirstLoad = true;
		private bool _isPolling = false;
		private object _pageTurnRoutine;

		private GameObject TagFrame;
		private List<TagHolder> _displayedFriendTags = new();
		private GameObject HoldButton;
		private List<GameObject> HoldButtons = new();
		private GameObject _scrollBar;

		private List<string> _previousList = new();
		private GameObject _selectedTag = new();
		private TagHolder _selectedFriend;

		private static GameObject IndicatorsBase;
		public static GameObject GetIndicator()
		{
			return GameObject.Instantiate(IndicatorsBase);
		}
		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			_sceneName = sceneName.ToLower();
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
			Instance = this;
		}
		/// <summary>
		/// Called when map is fully initialized reducing the risk of null references.
		/// </summary>
		private void OnMapInitialized()
		{
			Log(_sceneName, true);
			//addButtonsToFriendsScreen();
			if (_sceneName == "gym")
			{
				_scrollBar = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.FriendScreen.FriendScrollBar.GetGameObject();
				_selectedTag = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.SettingsScreen.PlayerTags.PlayerTag201.GetGameObject();
				TagFrame = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();

			}
			if (_sceneName == "park")
			{
				_scrollBar = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.FriendScreen.FriendScrollBar.GetGameObject();
				_selectedTag = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.SettingsScreen.PlayerTags.PlayerTag201.GetGameObject();
				TagFrame = Calls.GameObjects.Park.LOGIC.Heinhouwserproducts.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
			}
			Log("Starting poll for player tags...", true);
			if (_sceneName == "gym" || _sceneName == "park")
			{



				if (isFirstLoad)
				{
					IndicatorsBase = GameObject.Instantiate(Calls.LoadAssetFromStream<GameObject>(this, "ObsAutoRecorder.Assets.obsasset", "Canvas"));
					GameObject.DontDestroyOnLoad(IndicatorsBase);
					IndicatorsBase.transform.GetChild(0).GetComponent<RawImage>().color = Color.black;
					IndicatorsBase.SetActive(false);
					IndicatorsBase.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
				}


				for (int i = 0; i < 4; i++)
				{
					_scrollBar.transform.GetChild(i).GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
					{
						if (_pageTurnRoutine != null)
						{
							//MelonCoroutines.Stop(_pageTurnRoutine);
							//UpdateDisplayedTags();
						}
						_pageTurnRoutine = MelonCoroutines.Start(PollPageTurnCoRoutine());
						

					});
				}


				_selectedFriend = new TagHolder() { TagObject = _selectedTag };
				_displayedFriendTags = GetPlayerTags();
				MelonCoroutines.Start(PollPlayerTagsCoroutine());
				_selectedFriend.InteractionButton.GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
				{
					if (_selectedFriend.WasPressed)
						return;

					MelonCoroutines.Start(DebounceCoRoutine(_selectedFriend));
					ToggleAutoRecord(_selectedFriend);
				});

				isFirstLoad = false;
			}

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
				AutoRecordList.RemoveAll(x => x.Split('-')[0].Trim().ToLower() == selected.PlayFabID.Trim().ToLower());
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

		private bool IsInAutoRecordList(TagHolder friend)
		{
			var targets = AutoRecordList.Where(x => x.Split('-')[0].Trim().ToLower() == friend.PlayFabID.Trim().ToLower()).ToList();
			if (targets.Count > 1)
			{
				Log($"Warning: More than one entry found for {friend.PlayFabID} in AutoRecord list", false, 1);
			}
			return targets.Count > 0;
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

			_previousList.Clear();
			_previousList = _displayedFriendTags.Select(x => x.PlayFabID).ToList();
			_isPolling = false;
		}
		void UpdateDisplayedTags()
		{
			foreach (TagHolder info in _displayedFriendTags)
			{
				info.AutoRecordable = IsInAutoRecordList(info);
				Log(info.PublicName, true);
			}
		}
		IEnumerator PollPageTurnCoRoutine()
		{
			float start = Time.realtimeSinceStartup;


			while (/*SameTagsAsLast() &&*/ Time.realtimeSinceStartup - start < 2)
			{
				yield return null;
				Log("\n", true);
				UpdateDisplayedTags();
			}
			_previousList.Clear();
			_previousList = _displayedFriendTags.Select(x => x.PlayFabID).ToList();
			
			

		}
		private bool SameTagsAsLast()
		{
			for (int i = 0; i < _previousList.Count; i++)
			{
				bool match = _previousList[i] == _displayedFriendTags[i].PlayFabID;
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

	}
}