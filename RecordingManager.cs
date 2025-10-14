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

		//OBS Recording states
		private PlayfabInfo CurrentRecordedPlayer { get; set; }
		private PlayfabInfo NextPlayerToRecord { get; set; }
		private PlayfabInfo ActivePlayerInArena { get; set; }

		private bool IsPaused { get; set; } = false;
		private bool ExternalRecording { get; set; } = false;
		
		//private bool QueuedForStopping { get; set; } = false;
		private bool ModInitiatedStop { get; set; } = false;
		private bool IsWaitingForLastRecordStop { get; set; } = false;

		private string LastSceneName { get; set; }



		private bool StartRequestedByMod = false;
		private bool StopRequestedByMod = false;
		private bool PauseRequestedByMod = false;
		private bool IsSafeToRequestStart = true;


		
		private bool StopRequestInProgress { get; set; } = false;

		//variables for kidnapping external recording
		float TimeOfLastExternalPause { get; set; }
		private bool ModRequestedPause { get; set; } = false;
		private bool ModInitiatedPause { get; set; } = false;
		private void ResetVariables()
		{
			IsPaused = false;
			ExternalRecording = false;
			ModInitiatedPause = false;
			//QueuedForStopping = false;
			ModInitiatedStop = false;
			CurrentRecordedPlayer = null;
			NextPlayerToRecord = null;

			StartRequestedByMod = false;
			StopRequestedByMod = false;
			PauseRequestedByMod = false;



			//IsSafeToRequestStart = false; Should only be set by events and requests
			//StopRequestInProgress = false; Should only be set by events and requests
		}
		private bool IsAutoRecordable(PlayfabInfo player)
		{
			Log($"IsAutoRecordable? Opponent BP: {player.BP} BP threshold: {RecordByBPThreshold.Value}");
			if (IsInAutoRecordList(player.ID)) { return true; }

			if (RecordByBPThreshold.Value == -1) { return false; }

			if (player.BP >= RecordByBPThreshold.Value) { return true; }

			return false;
		}

		private void SetRecordingState()
		{
			if (SceneName.Contains("map") && PlayerManager.instance.AllPlayers.Count > 1)
				ActivePlayerInArena = new PlayfabInfo(PlayerManager.instance.AllPlayers[1]);

			if (SceneName == "gym")
				ActivePlayerInArena = new PlayfabInfo("Howard", "0000000000000000", int.MaxValue);

			if (SceneName == "park")
				ActivePlayerInArena = new PlayfabInfo($"{PlayerManager.instance.AllPlayers.Count} park players", "-1");


			if (!OBS.IsConnected())
			{
				Log("No active websocket connection to OBS detected", false, 1);
				return;
			}

			if ((SceneName == "gym") && LastSceneName.Contains("map"))
			{
				FightSessionEnd();
			}


			if (SceneName.Contains("map") && (LastSceneName == "gym") && PlayerManager.instance.AllPlayers.Count > 1)
			{
				FightSessionStart();
			}
			LastSceneName = SceneName;
		}
		private void FightSessionEnd()
		{
			if(ExternalRecording)
				{ return; }


			if (PauseAfterMatch.Value)
				PauseRecording();
			if (!(_recordingWaitCor is null))
			{
				Log($"FightSessionEnd: Existing recording hold coroutine found. Cancelling.", false, 1);
				MelonCoroutines.Stop(_recordingWaitCor);
				_recordingWaitCor = null;
			}
			_recordingWaitCor = MelonCoroutines.Start(RecordingHoldCoroutine(RecordingPauseHoldTimeout.Value));
		}


		private void FightSessionStart()
		{
			//Skip if active recording is started externally
			if ((OBS.IsRecordingActive() || IsPaused) && ExternalRecording) { return; }

			//Skip is active player is not recordable
			if (!IsAutoRecordable(ActivePlayerInArena)) { Log($"Player {ActivePlayerInArena.ToString()} does not meet auto record criteria.", false, 0); return; }


			//At this point, recording hold coroutine should be stopped if currently active regardless of results
			if (!(_recordingWaitCor is null))
			{
				Log($"Fight Session Start: Cancelling recording hold coroutine");
				MelonCoroutines.Stop(_recordingWaitCor);
				_recordingWaitCor = null;
			}

			//Null currentRecorded player means no recording active. Start new one.
			if (CurrentRecordedPlayer is null)
			{
				RequestStartRecording(ActivePlayerInArena);
				return;
			}

			//If opponent found is the same as the opponent having a recording hold, continue recording
			if (ActivePlayerInArena.ID == CurrentRecordedPlayer.ID)
			{
				Log($"Previous opponent {CurrentRecordedPlayer.ToString()} found. Resuming recording");
				if (IsPaused)
				{
					ResumeRecording();
				}
				else
				{
					if (!OBS.IsRecordingActive())
					{
						Log($"No active recording on hold. Starting new recording for {ActivePlayerInArena.ToString()}");
						RequestStartRecording(ActivePlayerInArena);
					}
					else
					{
						Log("$Recording already active. No action");
					}
				}
				return;
			}
			//All other cases means recording is currently held for previous player


			Log($"Recording currently active (real obs status:  {OBS.IsRecordingActive()}, Pause status: {IsPaused}) for player {CurrentRecordedPlayer.Name}", false);
			RequestRecordingStop();
			RequestStartRecording(ActivePlayerInArena);


			




		}



		private void RequestStartRecording(PlayfabInfo player)
		{
			NextPlayerToRecord = player;
			if ((OBS.IsRecordingActive() || IsPaused) && !StopRequestInProgress)
			{
				Log($"RequestStartRecording: Start recording request started when recording is in progress with no prior request to stop", true, 1);
			}
			StartRequestedByMod = true;

			//Keep requesting record start until successful
			Task.Run(() =>
			{
				float startTime = Time.realtimeSinceStartup;
				int secondsToRetry = 5;
				bool succeeded = false;

				while (!succeeded && (Time.realtimeSinceStartup - startTime < secondsToRetry))
				{
					succeeded = OBS.StartRecord();
					Thread.Sleep(100);
					RequestResponse.GetRecordStatus req = OBS.GetRecordStatus();
					//check possible status mismatches as it is apparently possible with obs
					Log($"RequestRecordStart: OBS.StartRecord result: {succeeded}. Actual record status: {req.outputActive}. Duration: {req.outputDuration}. IsRecordingActive: {OBS.IsRecordingActive()} ", true, succeeded == req.outputActive ? 1 : 2);
					succeeded = req.outputActive;
				}

				if (succeeded)
				{
					Log($"RequestStartRecording: Success!", true, 0);
					player.RecordingStart = System.DateTime.Now;
					player.IsRecording = true;
					ExternalRecording = false;
					CurrentRecordedPlayer = NextPlayerToRecord;
					NextPlayerToRecord = null;

					StartRequestedByMod = false;

					//Stop recording hold coroutine
					if (_stopQueueCor != null)
					{
						MelonCoroutines.Stop(_stopQueueCor);
						_stopQueueCor = null;
					}

				}
				else
				{
					Log($"RequestRecordStart: Failed to start recording for player {player.ToString()}. Timeout.", false, 2);
					ResetVariables();
				}
			});
		}

		private void RequestRecordingStop()
		{
			StopRequestInProgress = true;
			OBS.StopRecord();
		}

		private void PauseRecording()
		{
			ModRequestedPause = true;
			OBS.PauseRecord();
		}

		private void ResumeRecording()
		{
			OBS.ResumeRecord();
		}

		private void onRecordPause()
		{
			IsPaused = true;



			//kidnapping mechanic
			if (!ModRequestedPause)
				TimeOfLastExternalPause = Time.realtimeSinceStartup;


			ModInitiatedPause = ModRequestedPause;
			ModRequestedPause = false;

			
		}
		private void onRecordResume()
		{
			if(ExternalRecording)
			{
				//If paused externally then resumed within 0.5 seconds, claim recording as mod-owned and no longer external
				ExternalRecording = !((Time.realtimeSinceStartup - TimeOfLastExternalPause) < 0.5f);
			}
			IsPaused = false;
		}
		private void onRecordStart(string outputPath)
		{
			IsPaused = false;
			if (!StartRequestedByMod)
			{
				Log("onRecordStart: Recording started externally", false, 1);
			}

			Log($"onRecordStart: Recording started for: {outputPath}");

			//Assume externally initiated recording if no start request from within mod
			ExternalRecording = !StartRequestedByMod;
			NextPlayerToRecord.RecordingOutputPath = outputPath;
		}

		public string GetSafeFilename(string filename)
		{

			return string.Join("", filename.Split(Path.GetInvalidFileNameChars()));

		}
		private void onRecordStop(string outputPath)
		{
			if (outputPath != CurrentRecordedPlayer.RecordingOutputPath)
			{
				//warn if outputpath does not match expected output path assigned to player. Use player-assigned outputpath
				Log($"onRecordStop: mismatch between event output path {outputPath} and CurrentRecordedPlayer.RecordingOutputPath: {CurrentRecordedPlayer.RecordingOutputPath}", false, 1);

			}
			if (DoAutoRename.Value)
			{
				RenameOutput(CurrentRecordedPlayer.RecordingOutputPath, AutoRenameString.Value, CurrentRecordedPlayer, false);
			}
			else
			{
				Task.Run(CheckUntilFileIsWritable);
				
			}
			ResetVariables();
		}

		private void onConnect()
		{
			//Use in case pause status is out of sync as in the case of starting the game with a paused OBS recording already running
			GetRecordStatus recordStatus = OBS.GetRecordStatus();
			Log($"GetRecordStatus: outputActive: {recordStatus.outputActive}, outputPaused: {recordStatus.outputPaused}");
			IsPaused = recordStatus.outputPaused;

			SetRecordingState();

		}

		private void onReplayBufferSaved(string outputPath)
		{
			Log($"Replay buffer saved to: {outputPath}");
			string newFileName = outputPath;

			if (!SceneName.Contains("map"))
			{

			}
			newFileName = RenameOutput(outputPath, "R- " + AutoRenameString.Value, ActivePlayerInArena, true);
			newFileName = System.IO.Path.GetFileName(newFileName);
			if (AddChapterMarkers.Value)
			{
				Log("Attempting to add chapter marker", true);
				var param = new { chapterName = newFileName };
				Task.Run(() => { OBS.SendRequest("CreateRecordChapter", param); Log("Chapter Marker Request Sent"); });

				Log("Adding Chapter Marker", true);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="oldOutputPath"> Location of the original file to be renamed. Should be the same as player.RecordingOutputPath if recording, and remain the same if it's a clip</param>
		/// <param name="newName"></param>
		/// <param name="player"></param>
		/// <param name="isReplay"></param>
		/// <returns></returns>
		private string RenameOutput(string oldOutputPath, string newName, PlayfabInfo player, bool isReplay = false)
		{
			//File renaming
			string newPath = "";

			string date = isReplay ? System.DateTime.Now.ToString(DateFormat.Value) : player.RecordingStart.ToString(DateFormat.Value);
			string time = isReplay ? System.DateTime.Now.ToString(TimeFormat.Value) : player.RecordingStart.ToString(TimeFormat.Value);


			Log($"Player name for file rename: {player.Name}");

			string newFileName = newName.Replace("{player}", $"{GetSafeFilename(player.Name)}").Replace("{date}", date).Replace("{time}", time);
			newPath = System.IO.Path.GetDirectoryName(oldOutputPath) + "/" + newFileName + System.IO.Path.GetExtension(oldOutputPath);
			int copyIndex = 1;


			while (System.IO.File.Exists(newPath))
			{
				newPath = System.IO.Path.GetDirectoryName(oldOutputPath) + "/" + newFileName + $" ({copyIndex})" + System.IO.Path.GetExtension(oldOutputPath);
				copyIndex++;
			}
			Task.Run(() =>
			{
				bool success = false;
				float startTime = Time.realtimeSinceStartup;
				float currentTime = Time.realtimeSinceStartup;
				int secondsToRetry = 5;
				while (!success && !(currentTime - startTime > secondsToRetry))
				{
					currentTime = Time.realtimeSinceStartup;
					try
					{

						System.IO.File.Move(oldOutputPath, newPath);
						oldOutputPath = newPath;
						success = true;
						Log($"Recording renamed to: {newFileName}", false);
					}

					catch (IOException ex)
					{
						Log($"IOException when renaming file: {ex.Message}. File Path: {newPath}", true, 2);


					}
					catch (System.Exception ex)
					{
						Log($"System Exception when renaming file: {ex.Message}. File Path: {newPath}", true, 2);


					}
				}
				if (!success)
				{
					Log($"Tried renaming file for {secondsToRetry} seconds. Giving up. ", false, 2);
				}
			});

			StopRequestInProgress = false;


			return newPath;
		}

		private void CheckUntilFileIsWritable()
		{
			//TODO: Do proper check when able
			Thread.Sleep(500);
			StopRequestInProgress = false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="duration"></param>
		/// <returns></returns>
		/// <remarks>TODO: Ensure that user-started recordings are not stopped</remarks>
		private IEnumerator RecordingHoldCoroutine(float duration)
		{
			Log($"RecordingHold: Stop Queue States: "/*QueuedForStopping: {{QueuedForStopping}}, */ + $"ExternalRecording {ExternalRecording}", true);
			yield return new WaitForSeconds(duration);
			Log($"RecordingHold: "/*QueuedForStopping: {QueuedForStopping}, */ + $"ExternalRecording {ExternalRecording}", true);
			if (!ExternalRecording)
			{
				RequestRecordingStop();
			}
			_stopQueueCor = null;
		}
	}
}