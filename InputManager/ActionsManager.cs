using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using OBS_Control_API;
using ObsAutoRecorder;

using static ObsAutoRecorder.DebugHelper;
namespace ObsAutoRecorder.InputManager;

internal static class ActionsManager
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="delay"></param>
	/// <returns></returns>
	internal static IEnumerator SaveReplayBuffer(float delay)
	{
		yield return new WaitForSeconds(delay);
		OBS.SaveReplayBuffer();
		yield break;
	}
	/// <summary>
	/// 
	/// </summary>
	internal static void SaveTimeStamp()
	{
		ObsAutoRecorder.Instance.AddNowStamp();
	}
	/// <summary>
	/// 
	/// </summary>
	static bool markersInCooldown = false;

	/// <summary>
	/// Create a chapter marker request in OBS
	/// </summary>
	/// <param name="chapterName"></param>
	private static IEnumerator AddChapterMarker(string chapterName)
	{

		if (!OBS.IsRecordingActive())
		{
			
			Log("AddChapterMarker: Recording not active. Cannot add chapter marker", true, 1);
			yield break;
		}
		//Don't add marker if a marker request was made in the same frame. 
		if (markersInCooldown)
		{
			Log("AddChapterMarker: Marker debounce active. Skipping", true, 1);
			yield break;
		}

		//turn on cooldown
		markersInCooldown = true;
		//Since 2025-11-29 18-17-06, chapter names have been submitted as empty strings and don't contain new file name. That same day at 17-30-51, it was still working. No changes have been made to the code that I remember. Adding "Clip" at the start to ensure empty strings don't get through
		Log("Attempting to add chapter marker", true);
		var param = new { chapterName = chapterName };
		Task.Run(() => { Log($"CreateChapterResponse: {OBS.SendRequest("CreateRecordChapter", param)}"); Log("Chapter Marker Request Sent"); });

		Log("Adding Chapter Marker", true);
		//turn off cooldown on the next frame
		yield return new WaitForFixedUpdate();
		markersInCooldown = false;

	}
}