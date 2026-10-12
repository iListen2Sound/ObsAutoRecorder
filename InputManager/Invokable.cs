using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ObsAutoRecorder.DebugHelper;

namespace ObsAutoRecorder.InputManager;

internal abstract class Invokable
{
	protected List<Action> OnActivateSubscribers { get; set; } = [];
	protected List<Action> OnDeactivateSubscribers { get; set; } = [];
	/// <summary>
	/// 
	/// </summary>
	/// <param name="subscriber"></param>
	internal void RegisterNewSubscriber(Action subscriber)
	{
		OnActivateSubscribers.Add(subscriber);
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="subscriber"></param>
	internal void UnRegisterSubscriber(Action subscriber)
	{
		bool found = OnActivateSubscribers.Remove(subscriber);
		if (!found)
			Warning("Tried to remove action but it doesn't exist in this delegate");

	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="isForExit"></param>
	protected void InvokeSubscribers(bool isForExit = false)
	{
		List<Action> currentList = isForExit ? OnDeactivateSubscribers : OnActivateSubscribers;
		for (int i = currentList.Count - 1; i >= 0; i--)
		{
			Action subscriber = currentList[i];
			try
			{
				subscriber?.Invoke();
			}
			catch (Exception ex)
			{

				System.Reflection.Assembly modAssembly = subscriber.Method.DeclaringType?.Assembly;
				if (modAssembly == null && currentList[i].Target != null)
				{
					modAssembly = subscriber.Target.GetType().Assembly;
				}
				string modName = modAssembly?.GetName().Name ?? "UnknownMod";

				Error("Error in one of the subscribers for this input!" +
					$"\n\nin: {modName}\n\n" +
					$"----STACK TRACE-----" +
					$"{ex.ToString()}");
			}
		}
	}

}
