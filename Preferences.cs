using MelonLoader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
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
		private MelonPreferences_Entry<bool> AddChapterMarkers;
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
			OBSAutoRecorderSettings = MelonPreferences.CreateCategory("ObsAutoRecorder");
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
			AddChapterMarkers = RecordingSettings.CreateEntry("Chapter Markers", true, null, "Enabling will write chapter markers to the output video if the format supports it (currently only Hybrid MP4)");
			TimeStampFile = RecordingSettings.CreateEntry("Write Timestamp File", true, null, "Enabling will write timestamps to a text file for when a replay buffer is saved. Only available when recording");
			TimestampOffset = RecordingSettings.CreateEntry("Offset Duration", 45, null, "Define a start offset for when the event you were clipping started");
			TimestampFormat = RecordingSettings.CreateEntry("Timestamp Format", "{offsettime}-{timestamp}", null, "Format how timestamps are saved to make it easier to paste into utilities like ffmpeg or MKVToolNix or YouTube descriptions. Possible values are: {offsettime}, {timestamp}, {offsetduration}");
			TimecodeFormat = RecordingSettings.CreateEntry("Timecode Format", @"HH:mm:ss.ff", null, "The format of the timecodes in the timestamp");
			//SuppressRBuffer = RecordingSettings.CreateEntry("Suppress Replay Buffer", false, "Suppress replay buffer when recording with timestamps");

			IndicatorSettings = MelonPreferences.CreateCategory("Indicator Settings");
			IndicatorSettings.SetFilePath(Path.Combine(USER_DATA, CONFIG_FILE));

			PreferMinimalIcon = IndicatorSettings.CreateEntry("Prefer Minimal Icon", false, null, "Prefer Minimal OBS Icon for Recording indicator (This is kinda broken)");
			ClippingIconVisibleByDefault = IndicatorSettings.CreateEntry("Clip Icon Default Visibility", true, null, "Make the replay buffer icon always visible. Otherwise, it's only shown to show an inactive replay buffer and blinks when a clip is saved");
			RockCamVisibility = IndicatorSettings.CreateEntry("Show Icons on Camera", true, null, "Make Icons Visible on Rock Cam and Legacy Cam");
			MainIconPosition = IndicatorSettings.CreateEntry("Main Icon Position", 0, null, "Position of OBS Icon along healthbar. Left to right from 0 to 100");
			ReplayIconOffset = IndicatorSettings.CreateEntry("Replay Icon Offset", 5f, null, "Offset of Replay Buffer Icon from main OBS Icon");

			//easter egg
			miscoar = MelonPreferences.CreateCategory("Misc ObsAutoRecorder");
			misc = miscoar.CreateEntry("Misc", 0);

		}


		private void FindDeprecatedConfs()
		{
			string[] lines = File.ReadAllLines(Path.Combine(USER_DATA, CONFIG_FILE));
			string depIndicator = "\"deprecated: ";
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Contains("Replay Prefix") && !(lines[i].Contains(depIndicator)))
				{
					Log($"Found unmarked deprecated config option: \"{lines[i]}\".", false, 1);
					Log("Marking...", false, 0);
					lines[i] = "\n#↓↓↓ No longer used. Please delete\n" + depIndicator + lines[i].TrimStart('\"');
				}
			}

			File.WriteAllLines(Path.Combine(USER_DATA, CONFIG_FILE), lines);
		}

		private void SaveSettings()
		{

			OBSAutoRecorderSettings.SaveToFile();
			AutoRenameSettings.SaveToFile();
			RecordingSettings.SaveToFile();
			IndicatorSettings.SaveToFile();
			miscoar.SaveToFile();
		}

		private void ReadSettings()
		{
			OBSAutoRecorderSettings.LoadFromFile();
			AutoRenameSettings.LoadFromFile();
			RecordingSettings.LoadFromFile();
			IndicatorSettings.LoadFromFile();

		}
	}
}
