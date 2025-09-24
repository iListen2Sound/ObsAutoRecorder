using MelonLoader;
using RumbleModdingAPI;
using UnityEngine;
using Il2CppRUMBLE.Social;
using Il2CppRUMBLE;
using OBS_Control_API;
using Il2CppTMPro;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.UI;
using Il2CppRUMBLE.Interactions.InteractionBase;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

using JetBrains.Annotations;
using System.Collections;


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
	public class FriendInfo
	{
		public string PlayFabID { get; private set; }
		public string PublicName { get; private set; }
		private GameObject _tagObject;
		public GameObject TagObject
		{ 	get { return _tagObject; }
			set 
			{ 
				PlayFabID = value.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.playFabMasterId;
				PublicName = value.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.publicName;
				_tagObject = value; 
			}
		}


	}
	public class ObsAutoRecorder : MelonMod
	{
		//Hold button location 
		//--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Settings Screen/InteractionButton (1)/
		string _sceneName;
		private static bool debugMode = true;
		bool isFirstLoad = true;
		GameObject TagFrame;
		List<FriendInfo> friendTags = new();
		GameObject HoldButton;
		List<GameObject> HoldButtons = new();

		List<string> _displayedFriends = new();
		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			_sceneName = sceneName.ToLower();
		}
		public override void OnLateInitializeMelon()
		{
			Calls.onMapInitialized += OnMapInitialized;
		}
		/// <summary>
		/// Called when map is fully initialized reducing the risk of null references.
		/// </summary>
		private void OnMapInitialized()
		{
			Log(_sceneName, true);
			addButtonsToFriendsScreen();
		}

		

		/// <summary>
		/// Scans the player tags collection and updates the displayed friend tags list.
		/// </summary>
		/// <remarks></remarks>
		/// <returns>true if all player tags are found and processed successfully; otherwise, false.</returns>
		bool FindPlayerTags()
		{
			List<string> foundPlayers = new();

			List<FriendInfo> friendInfos = new();
			TagFrame = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
			for (int i = 0; i < TagFrame.transform.childCount; i++)
			{
				
				FriendInfo friendInfo = new FriendInfo();
				
				friendInfo.TagObject = TagFrame.transform.GetChild(i).gameObject;

				string playFabID = TagFrame.transform.GetChild(i).GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.playFabMasterId;
				string publicName = TagFrame.transform.GetChild(i).GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.publicName;

				if(string.IsNullOrEmpty(playFabID))
					return false;


				foundPlayers.Add($"{playFabID} - {publicName}");


				//Log($"Found tag: {testString}", true);
			}
			_displayedFriends = foundPlayers;
			return true;
		}

		IEnumerator PollPlayerTagsCoroutine()
		{
			Log("Starting to poll for player tags...", true);
			while (!FindPlayerTags())
			{
				yield return null;
			}
			Log("\n", true);
			Log("\n" + string.Join("\n", _displayedFriends), true);
		}

		private void addButtonsToFriendsScreen()
		{
			if (_sceneName == "gym")
			{
				try
				{
					Log("Starting poll for player tags...", true);	
					MelonCoroutines.Start(PollPlayerTagsCoroutine());
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
		private void Log(string message, bool debugOnly = false)
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