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
	public class FriendInfo : MelonMod
	{
		//ideal location for autorecord status 0.2391 -0.0336 -0.0091
		//friendblock path --------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Friend Screen/Player Tags/Player Tag 2.0/InteractionButton/Meshes/
		//status block location: playertag 0 0 0 
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
		public bool AutoRecordable
		{
			get { return _autoRecordable; }
			set
			{
				_autoRecordable = value;
				
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

		public string GetFriendString()
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
			//0.0017 0.0017 0.0017
			RecordIcon.transform.localScale = new Vector3(0.0017f, 0.0017f, 0.0017f);
			RecordIcon.transform.localRotation = Quaternion.Euler(90, 0, 0);
			//new Color (R = .45, G = .31, B = .22)
			RecordIcon.transform.GetChild(0).GetComponent<RawImage>().color = new Color(0.45f, 0.31f, 0.22f, 1f);
		}

		public FriendInfo()
		{
			
			
		}
	}
	public class ObsAutoRecorder : MelonMod
	{
		//Hold button location 
		//--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Settings Screen/InteractionButton (1)/
		public static ObsAutoRecorder Instance { get; private set; }

		string _sceneName;
		private static bool debugMode = true;
		bool isFirstLoad = true;
		private GameObject TagFrame;
		private List<FriendInfo> _friendTags = new();
		private GameObject HoldButton;
		private List<GameObject> HoldButtons = new();

		private List<string> _previousList = new();
		private GameObject _selectedTag = new();
		private FriendInfo _selectedFriend;

		private static GameObject IndicatorsBase;
		public static GameObject GetIndicator()
		{ 			
			return GameObject.Instantiate(IndicatorsBase);
		}
		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			_sceneName = sceneName.ToLower();
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
			Log("Starting poll for player tags...", true);
			if (_sceneName == "gym")
			{
				//TODO: Fix Asset Bundles
				if (isFirstLoad)
				{
					IndicatorsBase = GameObject.Instantiate(Calls.LoadAssetFromStream<GameObject>(this, "ObsAutoRecorder.Assets.obsasset", "LogoCanvas"));
					GameObject.DontDestroyOnLoad(IndicatorsBase);
					IndicatorsBase.transform.GetChild(0).GetComponent<RawImage>().color = Color.black;
					IndicatorsBase.SetActive(false);
					IndicatorsBase.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
				}
				
				_selectedTag = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.SettingsScreen.PlayerTags.PlayerTag201.GetGameObject();
				_selectedFriend = new FriendInfo() { TagObject = _selectedTag };
				_friendTags = GetPlayerTags();
				MelonCoroutines.Start(PollPlayerTagsCoroutine());
				_selectedFriend.InteractionButton.GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
				{
					ToggleAutoRecord(_selectedFriend);
				});

				isFirstLoad = false;
			}

			
		}


		private List<FriendInfo> GetPlayerTags()
		{
			List<FriendInfo> friendInfos = new();
			TagFrame = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
			for (int i = 0; i < TagFrame.transform.childCount; i++)
			{
				FriendInfo friendInfo = new FriendInfo();
				friendInfo.TagObject = TagFrame.transform.GetChild(i).gameObject;
				friendInfos.Add(friendInfo);
			}
			return friendInfos;
		}


		IEnumerator PollPlayerTagsCoroutine()
		{
			Log("Starting to poll for player tags...", true);
			while (!IsFriendInfoLoaded())
			{
				
				yield return null;
			}
			Log("\n", true);
			Log("\n" + string.Join("\n", _previousList), true);
			foreach (FriendInfo info in _friendTags)
			{
				//info.StatusIcon.SetActive(false);
			}
		}
		/// <summary>
		/// Scans the player tags collection and updates the displayed friend tags list.
		/// </summary>
		/// <remarks></remarks>
		/// <returns>true if all player tags are found and processed successfully; otherwise, false.</returns>
		bool IsFriendInfoLoaded()
		{

			return _friendTags.TrueForAll(x => !(string.IsNullOrEmpty(x.PlayFabID)));
		}

		public void ToggleAutoRecord(FriendInfo selected)
		{
			selected.AutoRecordable = !selected.AutoRecordable;

		}
		

		private void addButtonsToFriendsScreen()
		{
			if (_sceneName == "gym")
			{
				try
				{
					
					Log("retrieving hold button...", true);
					if (isFirstLoad)
					{
						HoldButton = GameObject.Instantiate(Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.SettingsScreen.InteractionButton1.Button.GetGameObject());
						HoldButton.transform.localPosition = new Vector3(0, 0.4f, 0);
						HoldButton.transform.localScale = new Vector3(10, 10, 10);
						HoldButton.transform.localRotation = Quaternion.Euler(0, 270, 0);
						GameObject.DontDestroyOnLoad(HoldButton);
						HoldButton.SetActive(false);
						isFirstLoad = false;
					}
				}
				catch (System.Exception ex)
				{
					Log($"Error during OnMapInitialized: {ex}", false);
				}
				Log("Adding hold buttons to list...", true);
				HoldButtons.Clear();
				for (int i = 0; i < TagFrame.transform.childCount; i++)
				{
					HoldButtons.Add(GameObject.Instantiate(HoldButton));
					HoldButtons[i].transform.SetParent(TagFrame.transform.GetChild(i).GetChild(0).GetChild(0).GetChild(0).transform, false);
					HoldButtons[i].SetActive(true);
				}
			}
		}

		public override void OnFixedUpdate()
		{

		}


		/// <summary>
		/// Logs a message to the console
		/// </summary>
		/// <param name="message"></param>
		/// <param name="debugOnly"></param>
		public void Log(string message, bool debugOnly = false)
		{
			if (!debugOnly)
			{
				LoggerInstance.Msg(message);
				return;
			}
			if (debugMode)
				LoggerInstance.Msg(message);
		}

	}
}