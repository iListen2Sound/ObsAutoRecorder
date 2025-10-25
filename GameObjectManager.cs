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
		private GameObject RecentTags;
		private List<TagHolder> _displayedFriendTags = new();
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
		private GameObject _recordingIndicatorBase;
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

			MelonCoroutines.Start(ExternalRecordingBlinkerCoroutine());
		}

		private void BuildPlayerIndicators()
		{
			PlayerUi = PlayerManager.Instance.LocalPlayer.Controller.gameObject.transform.GetChild(6).GetChild(0).gameObject;
			Log("PlayerUI loaded", true);
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

			OBSIcon.transform.SetParent(PlayerUi.transform, false);
			OBSIcon.transform.localPosition = new Vector3(-0.24f, 0.035f, 0.945f);
			OBSIcon.transform.localRotation = Quaternion.Euler(70, 150, 180);
			OBSIcon.transform.localScale = new Vector3(0.03f, 0.0001f, 0.03f);
			OBSIcon.SetActive(false);

			MinimalLogo = GameObject.Instantiate(LogoPack.transform.GetChild(2).GetChild(0).gameObject);
			MinimalLogo.SetName("OBS Icon Minimal");
			MinimalLogo.transform.SetParent(PlayerUi.transform, false);
			MinimalLogo.transform.localPosition = new Vector3(-0.24f, 0.035f, 0.945f);
			MinimalLogo.transform.localRotation = Quaternion.Euler(70, 150, 180);
			MinimalLogo.transform.localScale = new Vector3(0.03f, 0.0001f, 0.03f);
			MinimalLogo.SetActive(false);
			MinimalLogo.layer = RockCamVisibility.Value ? 0 : VR_ONLY_LAYER;

			ReplayBufferLogo = GameObject.Instantiate(LogoPack.transform.GetChild(3).gameObject);
			ReplayBufferLogo.SetName("Replay Buffer Icon");
			ReplayBufferLogo.transform.SetParent(PlayerUi.transform, false);
			//20 340 0
			ReplayBufferLogo.transform.localRotation = Quaternion.Euler(20, 333, 0);
			//-0.214 0.027 0.956
			ReplayBufferLogo.transform.localPosition = new Vector3(-0.214f, 0.027f, 0.956f);
			//0.03 0.03 0.03
			ReplayBufferLogo.transform.localScale = new Vector3(0.015f, 0.015f, 0.015f);
			ReplayBufferLogo.SetActive(true);
			ReplayBufferLogo.layer = RockCamVisibility.Value ? 0 : VR_ONLY_LAYER;

			DebugUi = Calls.Create.NewText(" ##LOGDIFF: Record: True, Pause: False, External Recording: False, \nFighterInMap: Tacoslayer, LastRecorded: Tacoslayer", 1f, Color.white, new Vector3(0f, 0.1f, 1f), Quaternion.Euler(0, 0, 0));
			DebugUi.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
			DebugUi.transform.localPosition = new Vector3(0f, 0.1f, 0.96f);
			DebugUi.transform.SetParent(PlayerUi.transform, false);
			DebugUiText = DebugUi.GetComponent<TextMeshPro>();
			DebugUi.SetActive(isDebugMode.Value);
			

		}

		private void BuildTagHolders()
		{
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
			_selectedFriend.IsSelected = true;
		}

		private List<TagHolder> GetPlayerTags()
		{
			List<TagHolder> friendInfos = new();


			//doesn't work because IsFriendInfoLoaded() checks if every displayed tag has text to return true.
			
			/*for (int i = 0; i < RecentTags.transform.childCount; i++)
			{
				TagHolder friendInfo = new TagHolder();
				friendInfo.TagObject = RecentTags.transform.GetChild(i).gameObject;
				friendInfos.Add(friendInfo);
				friendInfo.InteractionButton.GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
				{
					_selectedFriend.AutoRecordable = IsInAutoRecordList(friendInfo);
				});
			}*/

			for (int i = 0; i < TagFrame.transform.childCount; i++)
			{
				TagHolder friendInfo = new TagHolder();
				friendInfo.TagObject = TagFrame.transform.GetChild(i).gameObject;
				friendInfos.Add(friendInfo);

				//Event handler to update the selected fighter's auto-recordable status
				friendInfo.InteractionButton.GetComponent<InteractionButton>().onPressed.AddListener((System.Action)delegate
				{
					_selectedFriend.AutoRecordable = IsInAutoRecordList(friendInfo);
				});
			}
			

			return friendInfos;

		}

		//			
	}
}