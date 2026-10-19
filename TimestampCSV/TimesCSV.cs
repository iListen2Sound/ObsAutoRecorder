using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static ObsAutoRecorder.DebugHelper;
using System.Globalization;
using OBS_Control_API;
using Il2CppPlayFab.ProgressionModels;
namespace ObsAutoRecorder.TimestampCSV;

//{time}
internal static class TimesCSV
{
	private static string sessionString;
	internal static string TimeFolderPath => ObsAutoRecorder.Instance.TimeStampFileLocation?.Value ?? Path.Combine(ObsAutoRecorder.USER_DATA, "TimeFIles");
	internal static string FilePath => Path.Combine(TimeFolderPath, $"{sessionString}.csv");
	internal static int PreOffset => ObsAutoRecorder.Instance?.TimestampOffset?.Value ?? 0;
	internal static int PostOffset => 0;
	internal static string RowDefinition =>
	"StreamTime," +
	"Title," +
	"PreOffset," +
	"PostOffset," +
	"Date";


	// internal static TimeSpan Marker { get; set; }
	// internal static int PreOffset => ObsAutoRecorder.Instance?.TimestampOffset ?? 0;
	// internal static TimeSpan PostOffset { get; set; }
	// internal static string Title { get; set; }


	internal static void WriteRow(string title, TimeSpan StreamTime = default, TimeSpan RecordTime = default)
	{

		Directory.CreateDirectory(TimeFolderPath);
		if (!File.Exists(FilePath))
		{
			File.AppendAllText(FilePath, RowDefinition + "\n");
		}

		string row =
			$"{(StreamTime == default ? "NULL" : StreamTime.ToString("c", CultureInfo.InvariantCulture))}," +
			$"{CsvSanitize(title)}," +
			$"{PreOffset}," +
			$"{PostOffset}," +
			$"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

		Log($"Row to write: {row}");
		File.AppendAllText(FilePath, row + "\n");

	}

	private static string CsvSanitize(string value)
	{
		bool quotes =
			value.Contains(',') ||
			value.Contains('"') ||
			value.Contains('\n') ||
			value.Contains('\r');

		if (!quotes)
			return value;

		return "\"" + value.Replace("\"", "\"\"") + "\"";

	}

	internal static void CreateStreamStamp()
	{
		RequestResponse.GetStreamStatus currentStreamStatus = OBS.GetStreamStatus();

		if (!currentStreamStatus.outputActive)
		{
			Warning("No active stream");
			return;
		}

		WriteRow("Live Clip", TimeSpan.FromMilliseconds(currentStreamStatus.outputDuration));

	}
	internal static void Initialize()
	{
		sessionString = DateTime.UtcNow.ToString("s", CultureInfo.InvariantCulture);
		InputEvents.DebugTestKeyDown += CreateStreamStamp;
	}
}