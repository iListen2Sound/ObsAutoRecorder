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

	public class ObsAutoRecorder : MelonMod
	{
		//Hold button location 
		//--------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Settings Screen/InteractionButton (1)/
		string _sceneName;
		private static bool debugMode = true;
		GameObject tagsCollection;
		List<GameObject> DisplayedFriendTags = new();
		GameObject HoldButton;
		List<GameObject> HoldButtons;

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
			if (_sceneName == "gym" || _sceneName == "park")
			{
				MelonCoroutines.Start(PollPlayerTagsCoroutine());
				//TODO: fix null ref
				HoldButton = GameObject.Instantiate(Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.SettingsScreen.InteractionButton1.Button.GetGameObject());
				for (int i = 0; i < tagsCollection.transform.childCount; i++)
				{
					HoldButtons.Add(GameObject.Instantiate(HoldButton));
					HoldButtons[i].transform.SetParent(tagsCollection.transform.GetChild(i).transform);
				}
			}

			

			
		}

		

		/// <summary>
		/// Scans the player tags collection and updates the displayed friend tags list.
		/// </summary>
		/// <remarks></remarks>
		/// <returns>true if all player tags are found and processed successfully; otherwise, false.</returns>
		bool FindPlayerTags()
		{
			List<string> foundPlayers = new();
			tagsCollection = Calls.GameObjects.Gym.LOGIC.Heinhouserproducts.Telephone20REDUXspecialedition.FriendScreen.PlayerTags.GetGameObject();
			for (int i = 0; i < tagsCollection.transform.childCount; i++)
			{
				string playFabID = tagsCollection.transform.GetChild(i).GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.playFabMasterId;
				string publicName = tagsCollection.transform.GetChild(i).GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.publicName;

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
