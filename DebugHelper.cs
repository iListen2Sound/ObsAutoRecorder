using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObsAutoRecorder;

internal static class DebugHelper
{
	internal static bool DebugMode => ObsAutoRecorder.Instance?.isDebugMode?.Value ?? true;
	private static string lastDiffLogMessage = string.Empty;
	internal static void LogDiff(string message, bool debugOnly = true, int logLevel = 0)
	{
		if (message != lastDiffLogMessage)
		{
			lastDiffLogMessage = message;
			Log("DIFFLOG: " + message, debugOnly, logLevel);
		}
	}
	[DebuggerStepThrough]
	internal static void Log(string message, bool debugOnly = true, int logLevel = 0)
	{
		if (!DebugMode && debugOnly)
			return;
		switch (logLevel)
		{
			case 1:
				Melon<ObsAutoRecorder>.Logger.Warning(message);
				break;
			case 2:
				Melon<ObsAutoRecorder>.Logger.Error(message);
				break;
			default:
				Melon<ObsAutoRecorder>.Logger.Msg(message);
				break;
		}
	}

	[DebuggerStepThrough]
	internal static void Deb(string message)
	{
		Log(message, true, 0);
	}
	[DebuggerStepThrough]
	internal static void Msg(string message)
	{
		Log(message, false, 0);
	}
	[DebuggerStepThrough]
	internal static void Warning(string message)
	{
		Log(message, false, 1);
	}
	[DebuggerStepThrough]
	internal static void Error(string message)
	{
		Log(message, false, 2);
	}

}

