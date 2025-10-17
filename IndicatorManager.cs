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
using System.Threading;
using static OBS_Control_API.RequestResponse;
using Il2CppPlayFab.EconomyModels;
using Il2CppSystem;

namespace ObsAutoRecorder
{
	
	public partial class ObsAutoRecorder : MelonMod
	{

		private Color pauseColor = new Color(1f, 1f, 0f, 0.75f);
		private Color recordColor = new Color(1f, 1f, 1f, 0.75f);

		private Color errorColor = new Color(1f, 0f, 0f, 0.75f);


		private bool mainIconBlinker = false;
		private bool replayBufferBlinker = false;
		private bool recordPauseBlinker = false;

		private object _standbyBlink = null;
		private object _replayBufferBlink = null;

		private IEnumerator BlinkReplayBufferCoRoutine()
		{
			float startTime = Time.realtimeSinceStartup;
			float duration = 7;
			float interval = 0.3f;
			while((Time.realtimeSinceStartup - startTime) < duration)
			{
				replayBufferBlinker = !replayBufferBlinker;
				yield return new WaitForSeconds(interval);
			}
			replayBufferBlinker = false;
		}
		

		private IEnumerator ExternalRecordingBlinkerCoroutine()
		{
			while(true)
			{
				if(ExternalRecording)
				{
					mainIconBlinker = !mainIconBlinker;
				}
				else
				{
					mainIconBlinker = false;
				}
				yield return new WaitForSeconds(1f);
			}
		}
		private void SetIndicatorState()
		{
			//Log($"OBS.IsRecordingActive(): {OBS.IsRecordingActive()}\tIsPaused: {IsPaused}", true);
			if (PreferMinimalIcon.Value)
			{
				try
				{
					MinimalLogo.SetActive((OBS.IsRecordingActive() || IsPaused) ^ mainIconBlinker);
					MinimalLogo.GetComponent<MeshRenderer>().material.color = IsPaused ? pauseColor : recordColor;
				}
				catch (System.Exception ex)
				{
					Log($"SetIndicatorState: {ex.Message}", false, 2);

				}


			}
			else
			{
				try
				{
					OBSIcon.SetActive((IsPaused || OBS.IsRecordingActive()) ^ mainIconBlinker);
					PauseIcon.SetActive(IsPaused ^ recordPauseBlinker);
					//&&!IsPaused required due to inconsistency in OBS API. 
					RecordIcon.SetActive(OBS.IsRecordingActive() ^ recordPauseBlinker);

				}
				catch (System.Exception ex)
				{
					Log($"SetIndicatorState: {ex.Message}", false, 2);
				}
			}
			try
			{
				ReplayBufferLogo.SetActive(ClippingIconVisibleByDefault.Value ^ replayBufferBlinker);
				if(IsPaused)
				{
					ReplayBufferLogo.GetComponent<MeshRenderer>().material.color = pauseColor;
				}
				else
				{
					ReplayBufferLogo.GetComponent<MeshRenderer>().material.color = OBS.IsReplayBufferActive() ? recordColor : errorColor;
				}
			}catch(System.Exception ex)
			{

			}
			
		}
	}
}
