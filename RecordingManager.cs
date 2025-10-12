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

namespace ObsAutoRecorder
{

	public partial class ObsAutoRecorder : MelonMod
	{

		//OBS Recording states
		private PlayfabInfo CurrentRecordedPlayer {get; set;}
		private PlayfabInfo NextPlayerToRecord {get; set;}
		private bool IsPaused { get; set; } = false;
		private bool ModInitiatedRecording { get; set; } = false;
		private bool ModInitiatedPause { get; set; } = false;
		private bool QueuedForStopping { get; set; } = false;
		private bool ModInitiatedStop { get; set; } = false;
		private bool IsWaitingForLastRecordStop { get; set; } = false;

		private string LastSceneName {get; set;}



		private bool StartRequestedByMod = false;
		private bool StopRequestedByMod = false;
		private bool PauseRequestedByMod = false;
		private bool IsSafeToRequestStart = true;

		private void SetRecordingState()
		{
			if (!OBS.IsConnected())
			{
				Log("No active websocket connection to OBS detected", false, 1);
				return;
			}

			if ((SceneName == "gym") && !(LastSceneName == "gym" || LastSceneName == "park"))
			{
				WhenInGym();
				
			}


			if (SceneName.Contains("map") && (LastSceneName == "gym") && PlayerManager.instance.AllPlayers.Count > 1)
			{
				WhenInArena();
			} 
			LastSceneName = SceneName;
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
			PlayfabInfo opponent = new PlayfabInfo(PlayerManager.instance.AllPlayers[1]);
			

			

			if (IsAutoRecordable(opponent))
			{
				
				if (!(_recordingWaitCor is null))
				{
					MelonCoroutines.Stop(_recordingWaitCor);
					_recordingWaitCor = null;
				}

				if (CurrentRecordedPlayer.ID == opponent.ID)
				{
					Log($"Found previous opponent {opponent.Name}. ");
					if (IsPaused)
					{
						Log($"Resuming recording");
						ResumeRecording();
						QueuedForStopping = false;
					}
				}
				else
				{
					Log($"Found new opponent: {opponent.ToString()}. Replacing current recording", true);
					NextPlayerToRecord = opponent;
					if (OBS.IsRecordingActive() || IsPaused)
					{
						IsWaitingForLastRecordStop = true;
						StopRecording();
						if (_stopQueueCor != null)
						{
							MelonCoroutines.Stop(_stopQueueCor);
							_stopQueueCor = null;
						}
					}
						
					StartRecording(NextPlayerToRecord);
					Log($"Recording Started by  When in Arena logic {NextPlayerToRecord.ID} - {NextPlayerToRecord.Name}", true);

				}
			}
		}

		private bool IsAutoRecordable(PlayfabInfo player)
		{
			Log($"IsAutoRecordable? Opponent BP: {player.BP} BP threshold: {RecordByBPThreshold.Value}");
			if (IsInAutoRecordList(player.ID)) { return true; }

			if (RecordByBPThreshold.Value == -1) { return false; }

			if (player.BP >= RecordByBPThreshold.Value) { return true; }

			return false;
		}

		private void StartRecording(PlayfabInfo player)
		{

			if (OBS.IsRecordingActive() || IsPaused)
			{
				string pauseStatus = IsPaused ? "Paused " : "";
				Log($"Recording already in progress or paused", false, 1);
			}
			StartRequestedByMod = true;
			Log($"Starting recording for: {player.ID} - {player.Name}", false);
			
			QueuedForStopping = false;
			Task.Run(() =>
			{
				float startTime = Time.realtimeSinceStartup;
				int secondsToRetry = 5;
				bool success = false;


				while (!success && !(Time.realtimeSinceStartup - startTime > secondsToRetry) && !IsSafeToRequestStart)
				{
					
					if(!IsSafeToRequestStart)
					{
						Log("Awaiting previous recording clear", true, 1);
					}
					success = OBS.StartRecord();
					Thread.Sleep(250);
					if (success != OBS.IsRecordingActive())
					{
						Log("Mismatch between start recording status and IsRecordingActive. Retrying", true, 1);
						success = false;
					}
					else
					{
						Log("Match between start recording status and IsRecordingActive. Should be a success", true, 1);
					}
					
				}
				if(success)
				{
					Log("Recording started successfully", false);
					
					CurrentRecordedPlayer = NextPlayerToRecord;
					NextPlayerToRecord = null;
					
					
				}
				else
				{
					Log("Recording failed to start", false, 1);
				}
				
			});
		}

		private void StopRecording()
		{
			IsSafeToRequestStart = false;
			if (!(OBS.IsRecordingActive() || IsPaused))
			{
				Log("No recording in progress", true);
				return;
			}

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
			Log($"Recording paused for player: {CurrentRecordedPlayer.ToString()}");
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
			Log($"Recording Resumed for player: {CurrentRecordedPlayer.ToString()}");
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
			if(!StartRequestedByMod)
			{
				Log("Recording started externally", false, 1);
			}
			ModInitiatedRecording = StartRequestedByMod;
			StartRequestedByMod = false;
			Log($"Recording started for: {outputPath}");

			NextPlayerToRecord.RecordingOutputPath = outputPath;
			
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
			CurrentRecordedPlayer = null;
			
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