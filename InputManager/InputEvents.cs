using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using UnityEngine.UI;
using static ObsAutoRecorder.DebugHelper;

namespace ObsAutoRecorder;

internal static class InputEvents
{
	//Make always make sure to sort this according to assignment value
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
	internal static Dictionary<string, Chord> Combos { get; } = new Dictionary<string, Chord>
	{
		["XY"] = Chords[0],
		["AB"] = Chords[1],
		["AX"] = Chords[2],
	};
	static InputEvents()
	{

	}
	internal static void PollButtons()
	{
		//Log($"Polling Buttons....");
		foreach (Button button in Buttons)
		{
			button.UpdateCurrent();
		}
		PollTestKey();
	}

	internal static void AssignInput(Action method, params ControllerButtons[] digitalInputs)
	{

	}


	internal static void RegisterForChord(Action method, string chordComponents)
	{
		RemoveFromChord(method, "ALL");
		if (Combos.TryGetValue(chordComponents, out Chord value))
		{
			value.RegisterNewSubscriber(method);
		}
	}
	internal static void RemoveFromChord(Action method, string chordComponents)
	{
		if(chordComponents == "ALL")
		{
			foreach (Chord c in Chords)
			{
				c.UnRegisterSubscriber(method);
			}
			return;
		}

		
			

		if (Combos.TryGetValue(chordComponents, out Chord value))
		{
			
			value.UnRegisterSubscriber(method);
		}
	}

	internal static Action DebugTestKeyDown;
	static bool testKeyPressedLastFrame;
	static void PollTestKey()
	{
		if (!DebugHelper.DebugMode)
			return;
		//Log("Polling Test Key");
		bool testPress = Input.GetKeyDown(KeyCode.Semicolon);

		if(testPress && !testKeyPressedLastFrame)
		{
			DebugTestKeyDown?.Invoke();
			Log("TestKeyPressed");
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