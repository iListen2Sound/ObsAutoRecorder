using MelonLoader;
using System.IO;
using UIFramework.ValidatorExtensions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using System.ComponentModel.DataAnnotations;
using System;
namespace ObsAutoRecorder
{
	public partial class ObsAutoRecorder
	{
		private const string CONFIG_FILE = "config.cfg";

		private MelonPreferences_Category OBSAutoRecorderSettings;
		private MelonPreferences_Entry<bool> isDebugMode;

		private MelonPreferences_Category AutoRenameSettings;
		//private MelonPreferences_Entry<string> PlayersToRecord;
		private MelonPreferences_Entry<string> AutoRenameString;
		private MelonPreferences_Entry<string> ReplayAutoRenameString;
		private MelonPreferences_Entry<bool> DoAutoRename;
		private MelonPreferences_Entry<string> DateFormat;
		private MelonPreferences_Entry<string> TimeFormat;


		private MelonPreferences_Category RecordingSettings;

		private MelonPreferences_Entry<MarkerPrefs> AddMarkerOn;
		private MelonPreferences_Entry<int> RecordingPauseHoldTimeout;
		private MelonPreferences_Entry<int> RecordByBPThreshold;
		private MelonPreferences_Entry<bool> PauseAfterMatch;
		private MelonPreferences_Entry<bool> TimeStampFile;
		private MelonPreferences_Entry<int> TimestampOffset;
		private MelonPreferences_Entry<string> TimestampFormat;
		private MelonPreferences_Entry<bool> SuppressRBuffer;
		private MelonPreferences_Entry<string> TimecodeFormat;


		private MelonPreferences_Category IndicatorSettings;
		private MelonPreferences_Entry<bool> PreferMinimalIcon;
		private MelonPreferences_Entry<bool> ClippingIconVisibleByDefault;
		private MelonPreferences_Entry<bool> RockCamVisibility;
		private MelonPreferences_Entry<int> MainIconPosition;
		private MelonPreferences_Entry<float> ReplayIconOffset;


		private MelonPreferences_Category miscoar;
		private MelonPreferences_Entry<int> misc;

		private void InitPreferences()
		{
			OBSAutoRecorderSettings = MelonPreferences.CreateCategory("ObsAutoRecorder", "Misc Settings");
			OBSAutoRecorderSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			isDebugMode = OBSAutoRecorderSettings.CreateEntry("Debug Mode", false, null, "Enable debug with more verbose logging");

			AutoRenameSettings = MelonPreferences.CreateCategory("Auto Rename Settings");
			AutoRenameSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			DoAutoRename = AutoRenameSettings.CreateEntry("Enable Auto Rename", true, null, "Enable automatic renaming of recorded files");
			AutoRenameString = AutoRenameSettings.CreateEntry("Auto Rename String", "{date} {time} vs {player}", null, "Rename format for recorded files. Use {player}, {date}, {map}, and {time} as variables.");
			ReplayAutoRenameString = AutoRenameSettings.CreateEntry("Clip Auto Rename String", "R-{date} {time} vs {player}", null, "Rename format for saved replay buffer files");
			DateFormat = AutoRenameSettings.CreateEntry("Date Format", "yyyy-MM-dd", null, "Date format for renaming. https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
			TimeFormat = AutoRenameSettings.CreateEntry("Time Format", "HH-mm-ss", null, "Time format for renaming.");


			RecordingSettings = MelonPreferences.CreateCategory("Recording Settings");
			RecordingSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			RecordingPauseHoldTimeout = RecordingSettings.CreateEntry("Recording Hold Timeout", 0, null, "Seconds to keep the recording held before stopping automatically");
			PauseAfterMatch = RecordingSettings.CreateEntry("Pause recording after match", false, null, "Pause recording on returning to gym. Replay buffer will not work when paused");
			RecordByBPThreshold = RecordingSettings.CreateEntry("BP Threshold", -1, "BP", "Record players with BP greater than value. -1 = disabled");

			AddMarkerOn = RecordingSettings.CreateEntry("Add Marker On", MarkerPrefs.OnReplayBufferSaved, null, "When to add a chapter marker to the recording. Left and right bindings is pressing both the primary and secondary buttons on your controller.\n<i><color=\"yellow\"> Warning:</color></i> Certain VR Configurations don't support the left input combo.");


			TimeStampFile = RecordingSettings.CreateEntry("Write Timestamp File (Beta)", false, null, "Create a timestamp file when clipping while recording\n<i><color=\"yellow\"> BETA NOTICE:</color></i> Game restarts not handled. Will cause a new file to be written after the restart or even, in rare cases, overwrite the existing one.");
			TimestampOffset = RecordingSettings.CreateEntry("Offset Duration", 45, null, "Define a start offset for when the event you were clipping started");
			TimestampFormat = RecordingSettings.CreateEntry("Timestamp Format", "{offsettime}-{timestamp}", null, "Format how timestamps are saved to the file. Parameters: {offsettime}, {timestamp}, {offsetduration}");
			TimecodeFormat = RecordingSettings.CreateEntry("Timecode Format", @"hh\:mm\:ss\.ff", null, "The format of the timecodes in the timestamp");
			//SuppressRBuffer = RecordingSettings.CreateEntry("Suppress Replay Buffer", false, "Suppress replay buffer when recording with timestamps");

			IndicatorSettings = MelonPreferences.CreateCategory("Indicator Settings");
			IndicatorSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			PreferMinimalIcon = IndicatorSettings.CreateEntry("Prefer Minimal Icon", false, null, "Prefer Minimal OBS Icon for Recording indicator (This is kinda broken)", true);
			PreferMinimalIcon.Value = false; //untangle minimal icon from indicator settings
			ClippingIconVisibleByDefault = IndicatorSettings.CreateEntry("Clip Icon Default Visibility", true, null, "Make the replay buffer icon always visible. Otherwise, it's only shown to show an inactive replay buffer and blinks when a clip is saved");
			RockCamVisibility = IndicatorSettings.CreateEntry("Show Icons on Camera", true, null, "Make Icons Visible on Rock Cam and Legacy Cam");
			MainIconPosition = IndicatorSettings.CreateEntry("Main Icon Position", 0, null, "Position of OBS Icon along healthbar. Left to right from 0 to 100", false, false, new SliderDescriptor { DecimalPlaces = 0, Max = 100, Min = 0 });
			ReplayIconOffset = IndicatorSettings.CreateEntry("Replay Icon Offset", 5f, null, "Offset of Replay Buffer Icon from main OBS Icon", false, false, new SliderDescriptor { DecimalPlaces = 2, Max = 100, Min = -100 });

			//easter egg
			miscoar = MelonPreferences.CreateCategory("Misc ObsAutoRecorder");
			misc = miscoar.CreateEntry("Misc", 0);
			try
			{
				DeprecateAddChapterMarkers();
			}
			catch (Exception ex)
			{
				Log($"Error deprecating old settings: {ex.Message}", true, 1);

			}
		}

		internal enum MarkerPrefs
		{
			[Display(Name = "None", Description = "No marker will be added")]
			None,

			[Display(Name = "Replay Buffer Saves", Description = "Add marker when replay buffer is saved")]
			OnReplayBufferSaved,

			[Display(Name = "Right Binding", Description = "Add marker when right binding is activated")]
			OnRightCombo,

			[Display(Name = "Left Binding", Description = "Add marker when left binding is activated")]
			OnLeftCombo,
			

		}

		private void SaveSettings()
		{

			OBSAutoRecorderSettings.SaveToFile(false);
			AutoRenameSettings.SaveToFile(false);
			RecordingSettings.SaveToFile(false);
			IndicatorSettings.SaveToFile(false);
			miscoar.SaveToFile(false);
		}

		private void ReadSettings()
		{
			OBSAutoRecorderSettings.LoadFromFile(false);
			AutoRenameSettings.LoadFromFile(false);
			RecordingSettings.LoadFromFile(false);
			IndicatorSettings.LoadFromFile(false);

		}

		private void DeprecateAddChapterMarkers()
		{
			MelonPreferences_Entry<bool?> AddChapterMarkers = RecordingSettings.CreateEntry<bool?>("Chapter Markers", null, null, "[DEPRECATED use the new AddMarkerOn setting]", true);
			Log("Checking for deprecated settings to migrate", false, 1);
			if (AddChapterMarkers.Value == true)
			{
				AddMarkerOn.Value = MarkerPrefs.OnReplayBufferSaved;
				Log("Chapter markers enabled. AddMarkerOn = OnReplayBufferSaved", false, 1);
			}
			else if (AddChapterMarkers.Value == false)
			{
				AddMarkerOn.Value = MarkerPrefs.None;
				Log("Chapter markers disabled. AddMarkerOn = None", false, 1);
			}
			else if (AddChapterMarkers.Value is null)
			{
				Log("Old Chapter marker settings not found. No migration needed", false, 0);
			}
			RecordingSettings.DeleteEntry(AddChapterMarkers.Identifier);
			RecordingSettings.SaveToFile();
		}
	}
}
