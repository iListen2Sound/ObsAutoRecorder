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
using Il2CppPhoton.Compression;
namespace ObsAutoRecorder
{

	public partial class ObsAutoRecorder : MelonMod
	{
		private int VR_ONLY_LAYER { get { return LayerMask.NameToLayer("PlayerFade"); } }



		private GameObject TagFrame;
		private List<TagHolder> _displayedFriendTags = new();
		private GameObject RecentTags;
		private List<TagHolder> _recentlyMetTags = new();
		private GameObject HoldButton;
		private List<GameObject> HoldButtons = new();
		private GameObject LogoPack { get; set; }
		private GameObject PauseIcon { get; set; }
		private GameObject RecordIcon { get; set; }

		private GameObject OBSIcon { get; set; }
		private GameObject MinimalLogo { get; set; }
		private GameObject ReplayBufferLogo { get; set; }

		private GameObject DebugUi;
		private TextMeshPro DebugUiText;

		private GameObject _scrollBar;
		private GameObject PlayerUi;
		//private GameObject _recordingIndicator;

		bool isFirstLoad = true;
		private bool _isPolling = false;
		bool _sceneIsLoaded = false;




		private List<string> _previousList = new();
		private GameObject _selectedTag = new();
		private TagHolder _selectedFriend;

		private void FirstLoad()
		{
			OBS.Connect();
			LogoPack = GameObject.Instantiate(Calls.LoadAssetFromStream<GameObject>(this, "ObsAutoRecorder.Assets.obsasset", "logopack"));
			Log("LogoPack loaded", true);
			//GameObject.DontDestroyOnLoad(LogoPack);
			LogoPack.transform.SetParent(DDOLParent.transform, false);
			LogoPack.SetActive(false);


			IndicatorsBase = LogoPack.transform.GetChild(1).gameObject;
			IndicatorsBase.SetName("OBS Logo");
			IndicatorsBase.transform.GetChild(0).GetComponent<RawImage>().color = Color.black;

			IndicatorsBase.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

			IndicatorsBase.SetActive(false);

			MelonCoroutines.Start(ExternalRecordingBlinkerCoroutine());
		}

		public float Remap(float s )
		{
			float a1 = 0f;
			float a2 = 100f;

			float b1 = -15f;
			float b2 = 15f;

			return b1 + ((float) s - a1) * (b2 - b1) / (a2 - a1);
		}

		private void BuildPlayerIndicators()
		{
			PlayerUi = PlayerManager.Instance.LocalPlayer.Controller.gameObject.transform.GetChild(6).GetChild(0).gameObject;
			Log("PlayerUI loaded", true);

			GameObject mainIconAnchor = new GameObject("OBSAutoRecorder-MainAnchor");
			GameObject replayIconAnchor = new GameObject("OBSAutoRecorder-SecondaryAnchor");

			//Extreme Left = 0, 345, 0
			//Extreme Right = 0, 15, 0
			
			mainIconAnchor.transform.localRotation = Quaternion.Euler(0, Mathf.Clamp(Remap(MainIconPosition.Value), -15f, 15f), 0);
			mainIconAnchor.transform.localPosition = new Vector3(0f, 0.025f, 0f);
			mainIconAnchor.transform.SetParent(PlayerUi.transform, false);

			replayIconAnchor.transform.rotation = Quaternion.Euler(0, Mathf.Clamp((Remap(ReplayIconOffset.Value) + 15), -30f, 30f), 0); 
			replayIconAnchor.transform.localPosition = new Vector3(0f, -0.005f, 0f);
			replayIconAnchor.transform.SetParent(mainIconAnchor.transform, false);

			OBSIcon = GameObject.Instantiate(LogoPack.transform.GetChild(0).gameObject);
			OBSIcon.SetName("OBS Icon");
			OBSIcon.layer = RockCamVisibility.Value ? 0 : VR_ONLY_LAYER;
			Log("OBSIcon loaded", true);

			PauseIcon = OBSIcon.transform.GetChild(0).gameObject;
			PauseIcon.transform.localPosition = new Vector3(0.4f, -5f, -0.4f);
			PauseIcon.transform.localRotation = Quaternion.Euler(270, 0, 0);
			PauseIcon.layer = RockCamVisibility.Value ? 0 : VR_ONLY_LAYER;
			Log("PauseIcon loaded", true);

			RecordIcon = OBSIcon.transform.GetChild(1).gameObject;
			RecordIcon.transform.localPosition = new Vector3(0.4f, -5f, -0.4f);
			RecordIcon.layer = RockCamVisibility.Value? 0 : VR_ONLY_LAYER;

			OBSIcon.transform.SetParent(mainIconAnchor.transform, false);
			OBSIcon.transform.localPosition = new Vector3(0, 0, 1f);
			OBSIcon.transform.localRotation = Quaternion.Euler(70, 180, 180);
			OBSIcon.transform.localScale = new Vector3(0.03f, 0.0001f, 0.03f);
			OBSIcon.SetActive(false);

			MinimalLogo = GameObject.Instantiate(LogoPack.transform.GetChild(2).GetChild(0).gameObject);
			MinimalLogo.SetName("OBS Icon Minimal");
			MinimalLogo.transform.SetParent(mainIconAnchor.transform, false);
			MinimalLogo.transform.localScale = new Vector3(0.03f, 0.0001f, 0.03f);
			MinimalLogo.SetActive(false);
			MinimalLogo.layer = RockCamVisibility.Value ? 0 : VR_ONLY_LAYER;

			ReplayBufferLogo = GameObject.Instantiate(LogoPack.transform.GetChild(3).gameObject);
			ReplayBufferLogo.SetName("Replay Buffer Icon");
			ReplayBufferLogo.transform.SetParent(replayIconAnchor.transform, false);
			ReplayBufferLogo.transform.localRotation = Quaternion.Euler(20, 0, 0);
			ReplayBufferLogo.transform.localPosition = new Vector3(0, 0, 1f);
			ReplayBufferLogo.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
			ReplayBufferLogo.SetActive(true);
			ReplayBufferLogo.layer = RockCamVisibility.Value ? 0 : VR_ONLY_LAYER;

			DebugUi = Calls.Create.NewText("Placeholder text. You shouldn't be seeing this without some UE Shenanigans\n or decompiled code. Doesn't count if it's you, Ava. I (probably) told you about this.", 1f, Color.white, new Vector3(0f, 0.1f, 1f), Quaternion.Euler(0, 0, 0));
			DebugUi.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
			DebugUi.transform.localPosition = new Vector3(0f, 0.1f, 0.96f);
			DebugUi.transform.SetParent(PlayerUi.transform, false);
			DebugUiText = DebugUi.GetComponent<TextMeshPro>();
			DebugUi.SetActive(isDebugMode.Value);
			

		}

		private void BuildTagHolders()
		{
			_selectedFriend = new TagHolder() { TagObject = _selectedTag };
			_displayedFriendTags = GetPlayerTags(TagFrame);
			_recentlyMetTags = GetPlayerTags(RecentTags);
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
				UpdateDisplayedTags();
			});
			_selectedFriend.IsSelected = true;
		}

		private List<TagHolder> GetPlayerTags(GameObject phoneFrame)
		{
			List<TagHolder> tagInfos = new();

			for (int i = 0; i < TagFrame.transform.childCount; i++)
			{
				TagHolder tagInfo = new TagHolder();
				tagInfo.TagObject = TagFrame.transform.GetChild(i).gameObject;
				tagInfos.Add(tagInfo);

				//Event handler to update the selected fighter's auto-recordable status
				tagInfo.InteractionButton.GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
				{
					_selectedFriend.AutoRecordable = IsInAutoRecordList(tagInfo);
				});
			}
			

			return tagInfos;

		}

		//			
	}
}