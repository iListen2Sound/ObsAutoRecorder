using RumbleModdingAPI.RMAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObsAutoRecorder;

internal class Chord
{
	internal Action Enter;
	internal Action Exit;
	internal List<Button> Buttons;
	internal Chord(params Button[] buttons)
	{
		Buttons = [.. buttons.OrderBy(x => x.Assignment).ToList()];

		Buttons[Buttons.Count - 1].ButtonDown += CheckButtons;

	}

	private void CheckButtons()
	{
		bool allPressed = true;
		foreach(Button b in Buttons)
		{
			if(!b.ThisFrameWasPressed)
			{
				allPressed = false;
				break;
			}
		}
		if (allPressed)
			Enter?.Invoke();
	}


}

internal class Button
{
	private static readonly Func<bool>[] ButtonPressGetters = new Func<bool>[]
	{
		() => Calls.ControllerMap.LeftController.GetPrimary() > 0.5f,
		() => Calls.ControllerMap.RightController.GetPrimary() > 0.5f,
		() => Calls.ControllerMap.LeftController.GetSecondary() > 0.5f,
		() => Calls.ControllerMap.RightController.GetSecondary() > 0.5f,

	};
	internal Action ButtonDown;
	internal Action ButtonUp;
	internal ControllerButtons Assignment { get; private set; }
	internal bool LastFrameWasPressed { get; private set; }
	internal bool ThisFrameWasPressed { get; private set; }

	internal Button(ControllerButtons assignment)
	{
		Assignment = assignment;
	}

	internal void UpdateCurrent()
	{
		ThisFrameWasPressed = ButtonPressGetters[((int)Assignment)]();

		if (!LastFrameWasPressed && ThisFrameWasPressed)
			ButtonDown?.Invoke();
		if (LastFrameWasPressed && !ThisFrameWasPressed)
			ButtonUp?.Invoke();
		LastFrameWasPressed = ButtonPressGetters[((int)Assignment)]();
	}
}
