namespace ObsAutoRecorder.Rewrite;
using Il2CppPhoton.Realtime;
using Il2CppRUMBLE;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Social;
using Il2CppRUMBLE.UI;
using Il2CppRUMBLE.Players;
using Il2CppTMPro;
using JetBrains.Annotations;
using MelonLoader;
using OBS_Control_API;
using System.IO;
using RumbleModdingAPI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.Video;
using Il2CppSteamworks;
using System.Threading.Tasks;
using System.Threading;
using static OBS_Control_API.RequestResponse;
using Il2CppPlayFab.EconomyModels;
//using Il2CppSystem;
using System;
using UnityEngine.Rendering;
using Player = Il2CppRUMBLE.Players.Player;
using RumbleModdingAPI;

public static class RecordQueueManager
{
    private static Queue<Recording> RecordingQueue = new();
    internal static Recording GetCurrentRecording()
    {
        if(RecordingQueue.Count == 0)
            return null; 
        
        return RecordingQueue.Peek();
    }

    internal static void QueueForRecording(Recording recording)
    {
        RecordQueue.Enqueue(recording);

        Recording currentRecording = RecordingQueue.Peek();
        if(currentRecording == recording)
        {
            currentRecording.StartRecording();
        }
    }

    /// <summary>
    /// Should only be called by a Recording object when it has stopped recording. 
    /// Do not call from anywhere else. 
    /// 
    /// </summary>
    internal static void RecordingHasStopped(Recording recording)
    {
        if(recording.IsStopped)
        {
            if(RecordingQueue.Peek() == recording)
            {
                RecordingQueue.Dequeue();
                RecordingQueue.Peek().StartRecording();
            }
        }
    }
}