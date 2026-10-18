using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static ObsAutoRecorder.DebugHelper;
using System.Globalization;
namespace ObsAutoRecorder.TimestampCSV;
//{time}
internal static class TimesCSV
{

	internal static string FilePath {get; set;}
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
		
		if(!File.Exists(FilePath))
		{
			File.AppendAllText(FilePath, RowDefinition);
		}
		File.AppendAllText(FilePath,
			$"{(StreamTime == default ? "NULL" : StreamTime.ToString("c", CultureInfo.InvariantCulture))}," +
			$"{CsvSanitize(title)}," +
			$"{PreOffset}," +
			$"{PostOffset}," +
			$"{DateTime.Now.ToString("c", CultureInfo.InvariantCulture)}"
		);

	}

	private static string CsvSanitize(string value)
	{
		bool quotes = 
			value.Contains(',') ||
			value.Contains('"') ||
			value.Contains('\n') ||
			value.Contains('\r');

		if(!quotes)
			return value;
		
		return "\"" + value.Replace("\"", "\"\"") + "\"";

	}


}