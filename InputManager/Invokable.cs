using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;
using static ObsAutoRecorder.DebugHelper;

namespace ObsAutoRecorder.InputManager;

internal abstract class Invokable
{
	bool isInCooldown = false;
	protected List<Action> OnActivateSubscribers { get; set; } = [];
	protected List<Action> OnDeactivateSubscribers { get; set; } = [];
	/// <summary>
	/// 
	/// </summary>
	/// <param name="subscriber"></param>
	internal void RegisterNewSubscriber(Action subscriber, bool isForExit = false)
	{
		if(isForRelease)
			OnDeactivateSubscribers.Add(subscriber);
		else
			OnActivateSubscribers.Add(subscriber);
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="subscriber"></param>
	/// <remarks>TODO: Distinguish between unsubscribing from exit list or enter list</remarks>
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
		if(isInCooldown)
		{
			Log("This input still in cooldown");
			return;
		}

		MelonCoroutines.Start(Debouncer());

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

	internal IEnumerator Debouncer()
	{
		if(isInCooldown)
			yield break;

		isInCooldown = true;

		yield return new WaitForSeconds(0.25f);
		isInCooldown = false;
	}

}
