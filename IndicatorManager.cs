using MelonLoader;
using OBS_Control_API;
using System.Collections;
using UnityEngine;


namespace ObsAutoRecorder
{
	
	public partial class ObsAutoRecorder : MelonMod
	{

		System.Random random = new System.Random();
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
				if(ExternalRecording && !PreferMinimalIcon.Value)
				{
					mainIconBlinker = !mainIconBlinker;
				}
				else
				{
					mainIconBlinker = false;
				}
				yield return new WaitForSeconds(0.7f);
			}
		}
		private void SetIndicatorState()
		{
			bool isRecording = OBS.IsRecordingActive();
			//Log($"OBS.IsRecordingActive(): {OBS.IsRecordingActive()}\tIsPaused: {IsPaused}", true);
			if (PreferMinimalIcon.Value)
			{
				try
				{
					MinimalLogo.SetActive((isRecording || IsPaused) ^ mainIconBlinker);
					if(!ExternalRecording)
					{
						MinimalLogo.GetComponent<MeshRenderer>().material.color = IsPaused ? pauseColor : recordColor;
					}
					else
					{
						MinimalLogo.GetComponent<MeshRenderer>().material.color = errorColor;
					}
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
					OBSIcon.SetActive((IsPaused || isRecording) ^ mainIconBlinker);
					PauseIcon.SetActive((IsPaused ^ recordPauseBlinker) && IsPaused);
					//&&!IsPaused required due to inconsistency in OBS API. 
					RecordIcon.SetActive((isRecording ^ recordPauseBlinker) && isRecording);
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
