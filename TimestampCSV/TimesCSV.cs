using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ObsAutoRecorder.DebugHelper;
namespace ObsAutoRecorder.TimestampCSV;
//{time}
internal static class TimesCSV
{

	internal static string FilePath {get; set;}
	internal static int PreOffset => ObsAutoRecorder.Instance?.TimestampOffset ?? 0;
	internal static int PostOffset => 0;
	internal static string RowDefinition => 
	"Marker," +
	"Title," + 
	"PreOffset," +
	"PostOffset";
	
	// internal static TimeSpan Marker { get; set; }
	// internal static int PreOffset => ObsAutoRecorder.Instance?.TimestampOffset ?? 0;
	// internal static TimeSpan PostOffset { get; set; }
	// internal static string Title { get; set; }

	internal static WriteRow(TimeSpan marker, string title)
	{
		
		if(!File.Exists(FilePath))
		{
			File.AppendAllText(FilePath, RowDefinition);
		}
		File.AppendAllText(FilePath, 
			$"{marker},"+
			$"{title},"+
			$"{PreOffset},"+
			$"{PostOffset}";
		)

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