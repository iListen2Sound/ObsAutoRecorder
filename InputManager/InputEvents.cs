using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RumbleModdingAPI.RMAPI;

namespace ObsAutoRecorder;

internal static class InputEvents
{
	static Button[] Buttons { get; } =
		{
		new (ControllerButtons.LeftPrimary),
		new (ControllerButtons.RightPrimary),
		new (ControllerButtons.LeftSecondary),
		new (ControllerButtons.RightSecondary),
		};
	static List<Chord> Chords { get; } =
		[
		new(Buttons[((int)ControllerButtons.LeftPrimary)], Buttons[((int)ControllerButtons.LeftSecondary)]),
		new(Buttons[((int)ControllerButtons.RightPrimary)], Buttons[((int)ControllerButtons.RightSecondary)]),
		new(Buttons[((int)ControllerButtons.LeftPrimary)], Buttons[((int)ControllerButtons.RightPrimary)])
		];
	static InputEvents()
	{
		
	}
	internal static void PollButtons()
	{
		foreach (Button button in Buttons)
		{
			button.UpdateCurrent();
		}
	}
}
internal enum ControllerButtons
{
	LeftPrimary,
	RightPrimary,
	LeftSecondary,
	RightSecondary,
}