using Il2CppPhoton.Realtime;
using Il2CppRUMBLE;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Social;
using Il2CppRUMBLE.UI;
using Il2CppRUMBLE.Players;
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
//using Il2CppSystem;
using System;
using UnityEngine.Rendering;
using Player = Il2CppRUMBLE.Players.Player;

namespace ObsAutoRecorder
{

	public partial class ObsAutoRecorder : MelonMod
	{


		private object _obsReconnectCor = null;


		private object _stopQueueCor = null;

		//OBS Recording states
		private PlayfabInfo LastRecordedPlayer { get; set; }
		//private PlayfabInfo NextPlayerToRecord { get; set; }
		private PlayfabInfo ActivePlayerInArena { get; set; }

		private bool IsPaused { get; set; } = false;
		private bool ExternalRecording { get; set; } = false;

		//private bool QueuedForStopping { get; set; } = false;
		private bool ModInitiatedStop { get; set; } = false;
		private bool IsWaitingForLastRecordStop { get; set; } = false;

		private int ParkPlayers { get { return PlayerManager.instance.AllPlayers.Count - 1; } }
		private void onPlayerSpawn(Player player)
		{
			if (SceneName == "park")
			{
				ActivePlayerInArena = new PlayfabInfo($"{ParkPlayers} park player{(ParkPlayers == 1 ? "" : "s")}", "-1");
			}

		}

		private string LastSceneName { get; set; } = "loader";

		string LatestOutputPath = "";


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
			Log("ResetVariables called", true);
			LatestOutputPath = "";
			//IsPaused = false;

			ModInitiatedPause = false;
			//QueuedForStopping = false;
			ModInitiatedStop = false;

			TempFileDir = "";

			//NextPlayerToRecord = null;

			//Do not reset request variables
			/*StartRequestedByMod = false;
			ExternalRecording = false;
			StopRequestedByMod = false;
			LastRecordedPlayer = null;
			PauseRequestedByMod = false;*/



			//IsSafeToRequestStart = false; Should only be set by events and requests
			//StopRequestInProgress = false; Should only be set by events and requests
		}
		private bool IsAutoRecordable(PlayfabInfo player)
		{

			Log($"IsAutoRecordable? {IsInAutoRecordList(player.ID)}, Opponent BP: {player.BP}, BP threshold: {RecordByBPThreshold.Value}");
			if (IsInAutoRecordList(player.ID)) { return true; }

			if (RecordByBPThreshold.Value == -1) { return false; }

			if (player.BP >= RecordByBPThreshold.Value) { return true; }

			return false;
		}
		private void StartTryReconnecting()
		{
			Log("Checking if OBS is connected...", true);
			if (_obsReconnectCor != null)
			{
				MelonCoroutines.Stop(_obsReconnectCor);
				_obsReconnectCor = null;
			}
			else
			{
				_obsReconnectCor = MelonCoroutines.Start(TryConnectCoroutine());
			}
		}
		private void SetRecordingState()
		{
			//StartTryReconnecting();
			
			if (SceneName.Contains("map") && PlayerManager.instance.AllPlayers.Count > 1)
				ActivePlayerInArena = new PlayfabInfo(PlayerManager.instance.AllPlayers[1]);

			if (SceneName == "gym")
				ActivePlayerInArena = new PlayfabInfo("Howard", "0000000000000000", int.MaxValue);

			if (SceneName == "park")
				ActivePlayerInArena = new PlayfabInfo($"{ParkPlayers} park player{(ParkPlayers == 1 ? "" : "s")}", "-1");


			if (!OBS.IsConnected())
			{
				Log("SetRecordingState: No active websocket connection to OBS detected", false, 1);
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

		}
		private void FightSessionEnd()
		{
			if (ExternalRecording)
			{ Log("FightSessionEnd: External Recording. Exiting"); return; }


			if (PauseAfterMatch.Value)
				PauseRecording();


			if (!(_stopQueueCor is null))
			{
				Log("Existing recording hold coroutine found. Continuing", true, 0);
			}
			else
				_stopQueueCor = MelonCoroutines.Start(RecordingHoldCoroutine(RecordingPauseHoldTimeout.Value));
		}


		private void FightSessionStart()
		{
			string lastPlayer = LastRecordedPlayer is null ? "null" : LastRecordedPlayer.ToString();
			Log($"FightSessionStart: LastRecordedPlayer: {lastPlayer} ActivePlayerInArena: {ActivePlayerInArena.ToString()}");

			//Skip if active recording is started externally
			if (ExternalRecording)
			{ Log("FightSessionStart: External Recording. Exiting"); return; }

			/*
			if ((OBS.IsRecordingActive() || IsPaused))
			{
				Log("FightSessionStart: External Recording. Exiting");
				return;
			}*/

			//Skip is active player is not recordable
			if (!IsAutoRecordable(ActivePlayerInArena)) { Log($"Player {ActivePlayerInArena.ToString()} does not meet auto record criteria.", false, 0); return; }


			//At this point, recording hold coroutine should be stopped if currently active regardless of results
			if (!(_stopQueueCor is null))
			{
				Log($"Fight Session Start: Cancelling recording hold coroutine");
				MelonCoroutines.Stop(_stopQueueCor);
				_stopQueueCor = null;
				recordPauseBlinker = false;
				mainIconBlinker = false;
			}


			//Null currentRecorded player means no recording active. Start new one.
			if (LastRecordedPlayer is null)
			{
				Log($"Starting new recording for player {ActivePlayerInArena.ToString()}", true);
				if (OBS.IsRecordingActive() || IsPaused)
				{
					Log($"FightSessionStart: Internal recording active but LastRecordedPlayer is null. Please report. IsRecordingActive: {OBS.IsRecordingActive()}, IsPaused: {IsPaused}, LastRecordedPlayer is null: {LastRecordedPlayer is null}", false, 1);
					RequestRecordingStop();
				}

				RequestStartRecording(ActivePlayerInArena);
				return;
			}

			//If opponent found is the same as the opponent having a recording hold, continue recording
			if (ActivePlayerInArena.ID == LastRecordedPlayer.ID)
			{
				Log($"Previous opponent {LastRecordedPlayer.ToString()} found. Resuming recording");
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


			Log($"FightSessionStart: Replacing recording currently active (real obs status:  {OBS.IsRecordingActive()}, Pause status: {IsPaused}) for player {LastRecordedPlayer.Name} with {ActivePlayerInArena.Name}", false);
			RequestRecordingStop();
			RequestStartRecording(ActivePlayerInArena);
		}

		private void RequestStartRecording(PlayfabInfo player)
		{
			//NextPlayerToRecord = player;
			if ((OBS.IsRecordingActive() || IsPaused) && !StopRequestInProgress)
			{
				Log($"RequestStartRecording: Start recording request started when recording is in progress with no prior request to stop", true, 1);
			}
			StartRequestedByMod = true;

			//Keep requesting record start until successful
			Task.Run(() =>
			{
				float startTime = Time.realtimeSinceStartup;
				int secondsToRetry = 10;
				bool succeeded = false;
				while (StopRequestInProgress && !((Time.realtimeSinceStartup - startTime) > secondsToRetry))
				{
					Thread.Sleep(100);
					Log($"RequestStartRecording: Stop request in progress. Waiting", true);
				}
				startTime = Time.realtimeSinceStartup;

				while (!succeeded && !((Time.realtimeSinceStartup - startTime) > secondsToRetry))
				{
					succeeded = OBS.StartRecord();
					Thread.Sleep(400);
					RequestResponse.GetRecordStatus req = OBS.GetRecordStatus();
					//check possible status mismatches as it is apparently possible with obs
					Log($"RequestRecordStart: OBS.StartRecord result: {succeeded}. Actual record status: {req.outputActive}. Duration: {req.outputDuration}. IsRecordingActive: {OBS.IsRecordingActive()} ", true, succeeded == req.outputActive ? 1 : 2);
					if (!req.outputActive)
						succeeded = false;
				}


				if (succeeded)
				{
					Log($"RequestStartRecording: Success!", true, 0);
					player.RecordingStart = System.DateTime.Now;
					player.RecordingOutputPath = LatestOutputPath;
					player.IsRecording = true;
					ExternalRecording = false;


					//NextPlayerToRecord = null;

					StartRequestedByMod = false;

					//Stop recording hold coroutine
					if (_stopQueueCor != null)
					{
						MelonCoroutines.Stop(_stopQueueCor);
						_stopQueueCor = null;
						recordPauseBlinker = false;
						mainIconBlinker = false;
					}
					LastRecordedPlayer = player;
					Log($"RequestStartRecording: LastRecordedPlayer = {LastRecordedPlayer.ToString()}", true);
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
			Log("Recording paused");
			IsPaused = true;



			//kidnapping mechanic
			if (!ModRequestedPause)
				TimeOfLastExternalPause = Time.realtimeSinceStartup;


			ModInitiatedPause = ModRequestedPause;
			ModRequestedPause = false;


		}
		private void onRecordResume()
		{
			Log("Recording resumed");
			if (ExternalRecording)
			{
				//If paused externally then resumed within 0.5 seconds, claim recording as mod-owned and no longer external
				float timeSinceLastPaused = (Time.realtimeSinceStartup - TimeOfLastExternalPause);
				Log($"OnRecordResume: Time from Last Pause: {timeSinceLastPaused}", true);
				//ExternalRecording = !( timeSinceLastPaused < 0.5f);
				if (timeSinceLastPaused < 0.5f)
				{
					ExternalRecording = false;
					Log("OnRecordResume: External recording resumed quickly. Claiming recording as mod-initiated", false);
					LastRecordedPlayer = ActivePlayerInArena;
					LastRecordedPlayer.RecordingOutputPath = LatestOutputPath;
					LastRecordedPlayer.RecordingStart = System.DateTime.Now;
				}
				else
				{
					Log("OnRecordResume: External recording resumed after delay. Remaining external", true, 1);
				}

			}
			IsPaused = false;
		}
		private void onRecordStart(string outputPath)
		{
			TimeFileName = outputPath;


			if (_stopQueueCor != null)
			{
				MelonCoroutines.Stop(_stopQueueCor);
				_stopQueueCor = null;
				recordPauseBlinker = false;
				mainIconBlinker = false;
			}
			LatestOutputPath = outputPath;
			IsPaused = false;
			if (!StartRequestedByMod)
			{
				Log("onRecordStart: Recording started externally", false, 1);

			}

			//Make sure howard has a start recording date
			if (ActivePlayerInArena.ID == "0000000000000000" || ActivePlayerInArena.ID == "-1")
			{
				if (ActivePlayerInArena.RecordingStart.Equals(DateTime.MinValue))
				{
					ActivePlayerInArena.RecordingStart = System.DateTime.Now;
				}
			}
			Log($"onRecordStart: Recording started for: {outputPath}");

			//Assume externally initiated recording if no start request from within mod
			ExternalRecording = !StartRequestedByMod;

			/*if (ExternalRecording)
				NextPlayerToRecord.RecordingOutputPath = outputPath;*/
		}

		public string GetSafeFilename(string filename)
		{

			return string.Join("", filename.Split(Path.GetInvalidFileNameChars()));

		}
		private void onRecordStop(string outputPath)
		{
			Log($"OnRecordStop: Recording stopped {outputPath}");
			if (_stopQueueCor != null)
			{
				MelonCoroutines.Stop(_stopQueueCor);
				_stopQueueCor = null;
				recordPauseBlinker = false;
				mainIconBlinker = false;
			}
			if (ExternalRecording)
			{
				ExternalRecording = false;
				return;
			}

			if (outputPath != LastRecordedPlayer.RecordingOutputPath)
			{
				//warn if outputpath does not match expected output path assigned to player. Use player-assigned outputpath
				Log($"onRecordStop: mismatch between event output path {outputPath} and LastRecordedPlayer.RecordingOutputPath: {LastRecordedPlayer.RecordingOutputPath}", false, 1);
				if(LatestOutputPath != outputPath)
				{
					LatestOutputPath = outputPath;
				}

			}
			if (DoAutoRename.Value)
			{
				RenameOutput(LastRecordedPlayer.RecordingOutputPath, AutoRenameString.Value, LastRecordedPlayer, false);
			}
			else
			{
				Task.Run(CheckUntilFileIsWritable);

			}
			ResetVariables();

			IsPaused = false;
		}

		private void onConnect()
		{
			//Use in case pause status is out of sync as in the case of starting the game with a paused OBS recording already running
			GetRecordStatus recordStatus = OBS.GetRecordStatus();
			Log($"OnConnect: GetRecordStatus: outputActive: {recordStatus.outputActive}, outputPaused: {recordStatus.outputPaused}");
			IsPaused = recordStatus.outputPaused;

			//If currently recording, assume external recording.


			SetRecordingState();
			ExternalRecording = (recordStatus.outputPaused || recordStatus.outputActive);
		}
		private void onDisconnect()
		{
			//StartTryReconnecting();
		}
		private void onReplayBufferSaved(string outputPath)
		{
			//Temporary timestamp file location for when recording is ongoing but with the location is unknown.
			if(TempFileDir == "")
			{
				TempFileDir = System.IO.Path.GetDirectoryName(outputPath);
			}
			AddNowStamp();

			if (!(_replayBufferBlink is null))
			{
				replayBufferBlinker = false;
				MelonCoroutines.Stop(_replayBufferBlink);
				_replayBufferBlink = null;
			}
			_replayBufferBlink = MelonCoroutines.Start(BlinkReplayBufferCoRoutine());

			Log($"Replay buffer saved to: {outputPath} from {SceneName}");
			string newFileName = outputPath;

			if (!SceneName.Contains("map"))
			{

			}
			if (DoAutoRename.Value)
			{
				newFileName = RenameOutput(outputPath, ReplayAutoRenameString.Value, ActivePlayerInArena, true);
			}
			newFileName = System.IO.Path.GetFileNameWithoutExtension(newFileName);
			if (AddChapterMarkers.Value)
			{
				//Since 2025-11-29 18-17-06, chapter names have been submitted as empty strings and don't contain new file name. That same day at 17-30-51, it was still working. No changes have been made to the code that I remember. Adding "Clip" at the start to ensure empty strings don't get through
				Log("Attempting to add chapter marker", true);
				var param = new { chapterName = "clip: " + newFileName};
				Task.Run(() => { Log($"CreateChapterResponse: {OBS.SendRequest("CreateRecordChapter", param)}"); Log("Chapter Marker Request Sent"); });

				Log("Adding Chapter Marker", true);
			}

			if(SceneName == "gym")
			{
				misc.Value++;
				miscoar.SaveToFile();
				Log($"OnReplayBufferSaved: Misc value: {misc.Value}", true);
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
			if (String.IsNullOrEmpty(oldOutputPath))
			{
				Log($"RenameOutput: Provided oldOutputPath is empty. Using latest output path", false, 1);

				oldOutputPath = LatestOutputPath;
			}


			string newPath = "";
			string playerName = player.Name;
			if (player.ID == "0000000000000000" && misc.Value == 111)
			{
				playerName = $"Howard 2,147,483,647 BP";
			}

			string date = isReplay ? System.DateTime.Now.ToString(DateFormat.Value) : player.RecordingStart.ToString(DateFormat.Value);
			string time = isReplay ? System.DateTime.Now.ToString(TimeFormat.Value) : player.RecordingStart.ToString(TimeFormat.Value);


			Log($"Player name for file rename: {player.Name}");
			string mapName;
			switch (SceneName)
			{
				case "map0":
					mapName = "Ring";
					break;
				case "map1":
					mapName = "Pit";
					break;
				case "gym":
					mapName = "Gym";
					break;
				case "park":
					mapName = "Park";
					break;
				default:
					mapName = "Unknown";
					break;
			}

			string newFileName = newName.Replace("{player}", $"{GetSafeFilename(playerName)}").Replace("{date}", date).Replace("{time}", time).Replace("{map}", mapName);

			newPath = System.IO.Path.GetDirectoryName(oldOutputPath) + "/" + newFileName + System.IO.Path.GetExtension(oldOutputPath);
			Task.Run(() =>
			{
				int copyIndex = 1;
				FileInfo fileInfo = new FileInfo(newPath);
				fileInfo.Directory.Create();

				while (System.IO.File.Exists(newPath))
				{
					Log($"File exists: {newPath} ", false, 1);
					newPath = System.IO.Path.GetDirectoryName(oldOutputPath) + "/" + newFileName + $" ({copyIndex})" + System.IO.Path.GetExtension(oldOutputPath);
					
					copyIndex++;
				}

				

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
						Log($"IOException when renaming file: {ex.Message}. File Path: {newPath}\n{ex.Message}", true, 2);
						if (ex.Message.ToLower().Contains("could not find file"))
						{
							break;
						}
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

				if(!isReplay) 
					FinalRename(newPath);

				if (SceneName == "gym")
				{
					if (!isReplay)
					{
						LastRecordedPlayer = null;
					}
					Log("LastRecordedPlayer = null", true, 0);
				}

				StopRequestInProgress = false;

			});



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
			//yield return new WaitForSeconds(duration);

			if (ExternalRecording)
				yield break;


			float starttime = Time.realtimeSinceStartup;
			float interval = 0.5f;
			if (PreferMinimalIcon.Value)
			{
				interval = 0.2f;
			}
			while ((Time.realtimeSinceStartup - starttime) < duration)
			{
				if (PreferMinimalIcon.Value)
				{
					mainIconBlinker = !mainIconBlinker;
				}
				recordPauseBlinker = !recordPauseBlinker;
				if (duration - (Time.realtimeSinceStartup - starttime) < 10)
					interval = 0.2f;
				else if (duration - (Time.realtimeSinceStartup - starttime) < 3)
					interval = 0.1f;

				yield return new WaitForSeconds(interval);
			}
			recordPauseBlinker = false;
			mainIconBlinker = false;
			Log($"RecordingHold: "/*QueuedForStopping: {QueuedForStopping}, */ + $"ExternalRecording {ExternalRecording}", true);
			if (!ExternalRecording)
			{
				RequestRecordingStop();
			}


			_stopQueueCor = null;


		}
		private IEnumerator TryConnectCoroutine()
		{
			while (!OBS.IsConnected())
			{
				Log("TryConnectCoroutine: Not Connected Attempting to connect to OBS", true);
				OBS.Connect();
				yield return new WaitForSeconds(10f);			
				
			}

			Log("TryConnectCoroutine: Connected to OBS", true);
			_obsReconnectCor = null;
			yield break;
		}
	}
}