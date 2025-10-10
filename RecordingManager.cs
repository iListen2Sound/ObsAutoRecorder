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

	public partial class ObsAutoRecorder : MelonMod
	{

		//OBS Recording states
		private string CurrentOrLastRecordedPlayer { get; set; } = "";
		private string NewWaitingPlayer { get; set; } = "";
		private bool IsRecording { get; set; } = false;
		private bool IsPaused { get; set; } = false;
		private bool ModInitiatedRecording { get; set; } = false;
		private bool ModInitiatedPause { get; set; } = false;
		private bool QueuedForStopping { get; set; } = false;
		private bool ModInitiatedStop { get; set; } = false;
		private bool IsWaitingForStop { get; set; } = false;



		private bool StartRequestedByMod = false;
		private bool StopRequestedByMod = false;
		private bool PauseRequestedByMod = false;

		private void SetRecordingState()
		{
			if (!OBS.IsConnected())
			{
				Log("No active websocket connection to OBS detected", false, 1);
				return;
			}

			if (SceneName == "gym")
			{
				WhenInGym();
			}


			if (SceneName.Contains("map") && PlayerManager.instance.AllPlayers.Count > 1)
			{
				WhenInArena();
			}
		}
		private void WhenInGym()
		{
			if ((OBS.IsRecordingActive() || IsPaused) && ModInitiatedRecording)
			{
				if (!QueuedForStopping)
				{

					if (PauseAfterMatch.Value)
					{
						PauseRecording();
					}

					QueuedForStopping = true;
					if (_stopQueueCor != null)
					{
						MelonCoroutines.Stop(_stopQueueCor);
						_stopQueueCor = null;
					}
					Log("Starting Stop Hold Coroutine");
					_stopQueueCor = MelonCoroutines.Start(RecordingHoldCoroutine(RecordingPauseHoldTimeout.Value));
				}
				else { Log($"Hold not started. QueuedForStopping = {QueuedForStopping}, IsPaused: {IsPaused}"); }

			}
		}


		private void WhenInArena()
		{
			var opp = PlayerManager.instance.AllPlayers[1];
			var oppId = opp?.Data?.GeneralData?.PlayFabMasterId;
			var oppName = opp?.Data?.GeneralData?.PublicUsername ?? "Unknown";
			int oppBp = opp.Data.GeneralData.BattlePoints;

			string opponentInfo = $"{oppId} - {oppName}";

			if (IsAutoRecordable(oppId, oppBp))
			{
				QueuedForStopping = false;
				if (!(_recordingWaitCor is null))
				{
					MelonCoroutines.Stop(_recordingWaitCor);
					_recordingWaitCor = null;
				}

				if (CurrentOrLastRecordedPlayer.Split(" - ")[0] == oppId && ModInitiatedRecording)
				{
					Log($"Found previous opponent {oppName}. ");
					if (IsPaused)
					{
						Log($"Resuming recording");
						ResumeRecording();
					}
				}
				else
				{
					Log($"Found new opponent: {opponentInfo}. Replacing current recording", true);
					NewWaitingPlayer = opponentInfo;
					StopRecording();
				}
			}
		}

		private bool IsAutoRecordable(string opponentInfo, int opponentBP)
		{
			if (IsInAutoRecordList(opponentInfo)) { return true; }

			if (RecordByBPThreshold.Value == 0) { return false; }

			if (opponentBP >= RecordByBPThreshold.Value) { return true; }

			return false;
		}

		private void StartRecording(string playerID = "")
		{

			if (OBS.IsRecordingActive() || IsPaused)
			{
				string pauseStatus = IsPaused ? "Paused " : "";
				Log($"Recording already in progress or paused", false);

				return;
			}
			StartRequestedByMod = true;
			Log($"Starting recording for: {playerID}", false);
			CurrentOrLastRecordedPlayer = playerID;
			QueuedForStopping = false;
			OBS.StartRecord();
		}

		private void StopRecording()
		{
			if (!(OBS.IsRecordingActive() || IsPaused))
			{
				Log("No recording in progress", true);
				return;
			}
			StopRequestedByMod = true;
			QueuedForStopping = false;

			OBS.StopRecord();

		}

		private void PauseRecording()
		{

			if (!(OBS.IsRecordingActive() || IsPaused))
			{
				Log("No recording in progress", true);
				return;
			}
			if (IsPaused)
				return;
			PauseRequestedByMod = true;
			OBS.PauseRecord();
		}

		private void ResumeRecording()
		{
			OBS.ResumeRecord();
			ModInitiatedPause = false;
			QueuedForStopping = false;
		}

		private void onRecordPause()
		{
			IsPaused = true;
			ModInitiatedPause = PauseRequestedByMod;
			PauseRequestedByMod = false;
			Log($"Recording paused for player: {CurrentOrLastRecordedPlayer}");
		}
		private void onRecordResume()
		{
			if (_stopQueueCor != null)
			{
				MelonCoroutines.Stop(_stopQueueCor);
				_stopQueueCor = null;
			}

			Log("Starting Stop Hold Coroutine");
			ModInitiatedRecording = true;
			IsPaused = false;
			QueuedForStopping = false;
			Log($"Recording Resumed for player: {CurrentOrLastRecordedPlayer}");
		}
		private void onRecordStart(string outputPath)
		{
			if (_stopQueueCor != null)
			{
				MelonCoroutines.Stop(_stopQueueCor);
				_stopQueueCor = null;
			}

			IsPaused = false;
			//IsRecording = true;
			ModInitiatedRecording = StartRequestedByMod;
			StartRequestedByMod = false;
			Log($"Recording started for: {outputPath}");
		}

		public string GetSafeFilename(string filename)
		{

			return string.Join("", filename.Split(Path.GetInvalidFileNameChars()));

		}
		private void onRecordStop(string outputPath)
		{
			Log($"onRecordStop ({outputPath})", true);
			Log($"Recording saved to: {outputPath}");
			ModInitiatedStop = StopRequestedByMod;
			StopRequestedByMod = false;
			if (!ModInitiatedStop)
				Log("Recording stopped Externally", false, 1);
			if (DoAutoRename.Value)
			{
				Log($"Recording renamed to {RenameOutput(outputPath, AutoRenameString.Value)}");

			}
			else
			{
				Log("AutoRename disabled. Saving file as-is");
			}

			//Reset recording states

			//IsRecording = false;
			IsPaused = false;
			ModInitiatedRecording = false;
			ModInitiatedPause = false;
			QueuedForStopping = false;
			ModInitiatedStop = false;
			CurrentOrLastRecordedPlayer = "";
			if (_stopQueueCor != null)
			{
				MelonCoroutines.Stop(_stopQueueCor);
				_stopQueueCor = null;
			}

			if (NewWaitingPlayer != "")
			{
				StartRecording(NewWaitingPlayer);
				Log($"Recording Started by onRecordingStop for {NewWaitingPlayer}", true);
				NewWaitingPlayer = "";
			}

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
			newFileName = RenameOutput(outputPath, "R- " + AutoRenameString.Value);
			newFileName = System.IO.Path.GetFileName(newFileName);
			if (AddChapterMarkers.Value)
			{
				Log("Attempting to add chapter marker", true);
				var param = new { chapterName = newFileName };
				Task.Run(() => { OBS.SendRequest("CreateRecordChapter", param); Log("Chapter Marker Request Sent"); });

				Log("Adding Chapter Marker", true);
			}
		}
	}
}