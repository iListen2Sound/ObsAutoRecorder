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


namespace ObsAutoRecorder
{
	public class TagHolder
	{
		public static bool isDebug = true;
		//ideal location for autorecord status 0.2391 -0.0336 -0.0091
		//friendblock path --------------LOGIC--------------/Heinhouser products/Telephone 2.0 REDUX special edition/Friend Screen/Player Tags/Player Tag 2.0/InteractionButton/Meshes/
		//status block location: playertag 0 0 0 
		public bool WasPressed { get; set; } = false;

		public GameObject RecordIconBlock { get; private set; }
		public GameObject RecordIcon { get; private set; }

		public GameObject PlatformStatus { get; set; }

		private GameObject NameBlock { get; set; }

		public bool IsSelected { get; set; } = false;

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
				/*if (!IsSelected)
				{
					PlatformStatus.SetActive(!_autoRecordable);
					RecordIcon.SetActive(_autoRecordable);
				}
				else
				{
					Log("Selected tag. disabling platform status", true, 0);
					PlatformStatus.SetActive(false);
					RecordIcon.SetActive(true);
				}*/
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
		public PlayfabInfo PlayFabinfo()
		{
			return new PlayfabInfo(TagObject);
		}

		private void CreateAutoRecordBlock()
		{
			//0130
			PlatformStatus = TagObject.transform.GetChild(0).GetChild(1).GetChild(3).GetChild(0).gameObject;
			RecordIconBlock = GameObject.Instantiate(TagObject.transform.GetChild(0).GetChild(0).GetChild(0).gameObject);
			RecordIconBlock.SetName("RecordIconBlock");

			RecordIconBlock.transform.SetParent(TagObject.transform.GetChild(0).GetChild(0), false);
			//-0.2644 0.0349 -0.0091
			RecordIconBlock.transform.localPosition = new Vector3(0.2656f, 0.0349f, -0.0091f);

			RecordIcon = ObsAutoRecorder.GetIndicator();
			RecordIcon.transform.SetParent(RecordIconBlock.transform, false);
			RecordIcon.SetActive(true);
			RecordIcon.transform.localPosition = new Vector3(0, 0.5f, 0);
			//0.0085 0.0085 0.0085
			RecordIcon.transform.localScale = new Vector3(0.0085f, 0.0085f, 0.0085f);
			RecordIcon.transform.localRotation = Quaternion.Euler(90, 0, 0);
			//new Color (R = .45, G = .31, B = .22)
			AutoRecordable = false;

			//nameblock
			//localscale 0.0037 0.0341 -0.0095
			//scale 0.0324 0.224 0.1285
			//003
			NameBlock = TagObject.transform.GetChild(0).GetChild(0).GetChild(3).gameObject;
			NameBlock.transform.localPosition = new Vector3(0.0037f, 0.0341f, -0.0095f);
			NameBlock.transform.localScale = new Vector3(0.0324f, 0.224f, 0.1285f);
		}

		public TagHolder()
		{


		}

		public static string Sanitize(string Input)
		{

			string pattern = @"<[^>]*>";
			return Regex.Replace(Input, pattern, string.Empty);
		}
		public void Log(string message, bool debugOnly = false, int logLevel = 0)
		{
			if (debugOnly && !isDebug)
				return;

			switch (logLevel)
			{
				case 1:
					Melon<ObsAutoRecorder>.Logger.Warning("Warn: " + message);
					break;
				case 2:
					Melon<ObsAutoRecorder>.Logger.Warning("Error: " + message);
					break;
				default:
					Melon<ObsAutoRecorder>.Logger.Warning(message);
					break;
			}
		}
	}
}
