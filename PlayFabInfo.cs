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

namespace ObsAutoRecorder
{
    public class PlayfabInfo
    {
        private string _name;
        public string Name 
        {
            get
            {
                return TagHolder.Sanitize(_name);
            } 
            set
            {
                _name = value;
            }
        }
        public string ID {get; set;}
        public int BP {get; set;}
        public string RecordingOutputPath {get; set;}
        public bool IsRecording {get; set; } = IsWaitingForLastRecordStop

        public PlayfabInfo(string name, string id, int bp) : this(namme, id)
        {
            BP = bp;
        }
        public PlayfabInfo(string name, string id)
        {
            Name = name;
            ID = id;
        }
        public PlayfabInfo(GameObject playerTag)
        {
            string name = return TagHolder.Sanitize(playerTag.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.publicName);
            string id = playerTag.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.playFabMasterId;
        }
        public PlayfabInfo(var playerController)
        {
            ID = playerController?.Data?.GeneralData?.PlayFabMasterId;
            Name = playerController?.Data?.GeneralData?.PublicUserName;
            BP = playerController?.Data.GeneralData.BattlePoints;
        }

        public PlayfabInfo(string fullPlayerString)
        {
            string[] idParts = fullPlayerString.Split(" - ");
            if(idParts.Length != 2)
            {
                throw new Exception("fullPlayerString does not match expected format \"{Playfab ID} - {Player Name}\"");
            }
            ID = idParts[0];
            Name = idParts[1];
            
        }
        public string ToString();
        {
            string fullString = $"{ID} - {Name}";
            return fullString;
        }
    }
}