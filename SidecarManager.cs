using Il2CppPhoton.Realtime;
using Il2CppRUMBLE;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Managers;
using Il2CppRUMBLE.Social;
using Il2CppRUMBLE.UI;
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
using Il2CppSystem;
using UnityEngine.Rendering;

namespace ObsAut  oRecorder
{

	public partial class ObsAutoRecorder : MelonMod
	{
        private List<string> Timestamps = new List();
        private string TimeFileName {get; set;} = "";
        private string TempFileDir {get; set;} = "";

        private void AddNowStamp()
        {
            TimeFileName = LatestOutputPath;
            
            
            Task.Run(() => 
            {
                RequestResponse.GetRecordStatus req = OBS.GetRecordStatus();
                if(!req.outputActive)
                {
                    Log($"AddNowStamp: Output not active.", true, 1)
                    return;
                }
                
                //Check if TimeFileName has been assigned a 
                if(TimeFileName == "")
                {
                    TimeFileName = $"Temp {AutoRenameString.Value}";
                    string playerName = player.Name;
                    if (player.ID == "0000000000000000" && misc.Value == 111)
                    {
                        playerName = $"Howard 2,147,483,647 BP";
                    }

                    string date = isReplay ? System.DateTime.Now.ToString(DateFormat.Value) : player.RecordingStart.ToString(DateFormat.Value);
                    string time = isReplay ? System.DateTime.Now.ToString(TimeFormat.Value) : player.RecordingStart.ToString(TimeFormat.Value);


                    Log($"Player name for file rename: {player.Name}");
                    string mapName;
                    switch (SceneName)
                    {
                        case "map0":
                            mapName = "Ring";
                            break;
                        case "map1":
                            mapName = "Pit";
                            break;
                        case "gym":
                            mapName = "Gym";
                            break;
                        case "park":
                            mapName = "Park";
                            break;
                        default:
                            mapName = "Unknown";
                            break;
			        }
                    TimeFileName = Path.Combine(TempFileDir, TimeFileName.Replace("{player}", $"{GetSafeFilename(playerName)}").Replace("{date}", date).Replace("{time}", time).Replace("{map}", mapName));

                }

                TimeFileName += ".txt";
                
                //Start retrieving recording data
                TimeSpan currentPlayTime = TimeSpan.FromMilliseconds(req.outputDuration);
                TimeSpan offsetDuration = TimeSpan.FromSeconds(TimeStampOffset.Value);
                TimeSpan offsetTime = currentPlayTime - offsetDuration;

                offsetTime = offsetTime >= 0 ? offsetTime : TimeSpan.Zero;

                string formattedEntry = TimestampFormat.Value.Replace("{timestamp}", currentPlayTime.ToString(TimecodeFormat.Value)).Replace("{offsetduration}", offsetDuration.ToString(TimecodeFormat.Value)).Replace("{offsettime}", offsetTime.ToString(TimecodeFormat.Value));
                TimeStamps.Add(formattedEntry);


                File.WriteAllLines(TimeFileName)
                
                

                
            });
        }

    }
}