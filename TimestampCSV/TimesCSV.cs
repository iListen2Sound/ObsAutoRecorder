using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ObsAutoRecorder.DebugHelper;
namespace ObsAutoRecorder.TimestampCSV;

internal static class TimesCSV
{

	

	private class TimeItem
	{
		internal TimeSpan Marker { get; set; }
		internal int PreOffset => ObsAutoRecorder.Instance?.TimestampOffset ?? 0;
		internal TimeSpan PostOffset { get; set; }
		internal string Title { get; set; }


	}

}