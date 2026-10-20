using Il2CppPlayFab.Internal;
using ObsAutoRecorder.InputManager;
using RumbleModdingAPI.RMAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ObsAutoRecorder.DebugHelper;

namespace ObsAutoRecorder.InputManager;
/// <summary>
/// 
/// </summary>
internal class Chord : Invokable
{
	internal List<ControllerButtons> ChordComponents { get; }
	public override string ToString()
	{
		return string.Join(", ", ChordComponents);
	}

	/*	internal Action OnActivate;
		internal Action OnDeactivate;*/
	private List<Button> Buttons;
	private bool wasEnteredLastCheck = false;
	/// <summary>
	/// 
	/// </summary>
	/// <param name="buttons"></param>
	internal Chord(params Button[] buttons)
	{
		//Sort buttons by their assignment
		Buttons = [.. buttons.OrderBy(x => x.Assignment).ToList()];

		//Only check other inputs when the final button in the polling order is pressed
		//This makes sure the other buttons have been registered as pressed for that frame already
		//Buttons[Buttons.Count - 1].RegisterNewSubscriber(CheckButtons);
		ChordComponents = Buttons.Select(b => b.Assignment).ToList();

		foreach (Button b in Buttons)
		{
			b.RegisterNewSubscriber(CheckButtons);
			b.RegisterNewSubscriber(CheckRelease, false);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	private void CheckButtons()
	{

		bool allPressed = true;
		foreach (Button b in Buttons)
		{
			if (!b.ThisFrameWasPressed)
			{
				allPressed = false;
				break;
			}
		}
		if (allPressed)
		{
			InvokeSubscribers();
			Log($"Chord Enter: {this.ToString()}");
			wasEnteredLastCheck = allPressed;
		}
	}

	private void CheckRelease()
	{
		if (!wasEnteredLastCheck)
			return;
		InvokeSubscribers(false);
		wasEnteredLastCheck = false;
		Log($"Chord Exit: {this.ToString()}");
	}
}