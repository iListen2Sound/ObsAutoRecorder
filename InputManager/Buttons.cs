using ObsAutoRecorder.InputManager;
using RumbleModdingAPI.RMAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObsAutoRecorder;

internal class Button : Invokable
{
    private static float pressThreshold = 0.8f;
    private static float releaseThreshold = 0.2f;
	private static readonly Func<bool>[] ButtonPressGetters = new Func<bool>[]
	{
		() => Calls.ControllerMap.LeftController.GetPrimary() > pressThreshold,
		() => Calls.ControllerMap.RightController.GetPrimary() > pressThreshold,
		() => Calls.ControllerMap.LeftController.GetSecondary() > pressThreshold,
		() => Calls.ControllerMap.RightController.GetSecondary() > pressThreshold,

	};
    private static readonly Func<bool>[] ButtonReleaseGetters = new Func<bool>[]
	{
		() => Calls.ControllerMap.LeftController.GetPrimary() > releaseThreshold,
		() => Calls.ControllerMap.RightController.GetPrimary() > releaseThreshold,
		() => Calls.ControllerMap.LeftController.GetSecondary() > releaseThreshold,
		() => Calls.ControllerMap.RightController.GetSecondary() > releaseThreshold,

	};
/*	internal Action OnActivate;
	internal Action OnDeactivate;*/
	internal ControllerButtons Assignment { get; private set; }
	internal bool LastFrameWasPressed { get; private set; }
	internal bool ThisFrameWasPressed { get; private set; }

	internal Button(ControllerButtons assignment)
	{
		Assignment = assignment;
	}

	/// <summary>
	/// 
	/// </summary>
	internal void UpdateCurrent()
	{
        //Compare button to the lighter threshold if button is already pressed. 
        ThisFrameWasPressed = LastFrameWasPressed ? ButtonReleaseGetters[((int)Assignment)]() : ButtonPressGetters[((int)Assignment)]();

		if (!LastFrameWasPressed && ThisFrameWasPressed)
			InvokeSubscribers();
		if (LastFrameWasPressed && !ThisFrameWasPressed)
			InvokeSubscribers();

		LastFrameWasPressed = ThisFrameWasPressed;
	}
}
