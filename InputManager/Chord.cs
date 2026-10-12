using RumbleModdingAPI.RMAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObsAutoRecorder;

internal class Chord
{
	internal List<ControllerButtons> ChordComponents {get;}

	internal Action Enter;
	internal Action Exit;
	private List<Button> Buttons;
	private bool wasEnteredLastCheck = false;

	internal Chord(params Button[] buttons)
	{
		//Sort buttons by their assignment
		Buttons = [.. buttons.OrderBy(x => x.Assignment).ToList()];

		//Only check other inputs when the final button in the polling order is pressed
		//This makes sure the other buttons have been registered as pressed for that frame already
		Buttons[Buttons.Count - 1].ButtonDown += CheckButtons;
		ChordComponents = Buttons.Select(b => b.Assignment);
		foreach (Button b in Buttons)
		{
			b.ButtonUp += CheckRelease();
		}
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
		{
			Enter?.Invoke();		
			wasEnteredLastCheck = allPressed;
		}
	}

	private void CheckRelease()
	{
		if (!wasEnteredLastCheck)
			return;
		
		Exit?.Invoke();
		wasEnteredLastCheck = false;
		
	}
}

