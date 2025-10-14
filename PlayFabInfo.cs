using UnityEngine;
using System;

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
		public string ID { get; set; }
		public int BP { get; set; }
		public string RecordingOutputPath { get; set; }
		public DateTime RecordingStart { get; set; }
		public bool IsRecording { get; set; } = false;

		public PlayfabInfo(string name, string id, int bp) : this(name, id)
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
			string name = TagHolder.Sanitize(playerTag.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.publicName);
			string id = playerTag.GetComponent<Il2CppRUMBLE.Social.Phone.PlayerTag>()._UserData_k__BackingField.playFabMasterId;
		}
		public PlayfabInfo(Il2CppRUMBLE.Players.Player playerController)
		{
			ID = playerController?.Data?.GeneralData?.PlayFabMasterId;
			Name = playerController?.Data?.GeneralData?.PublicUsername;
			BP = (int)(playerController?.Data.GeneralData.BattlePoints);
		}

		public PlayfabInfo(string fullPlayerString)
		{
			string[] idParts = fullPlayerString.Split(" - ");
			if (idParts.Length != 2)
			{
				throw new Exception($"fullPlayerString does not match expected format \"{ID} - {Name}\"");
			}
			ID = idParts[0];
			Name = idParts[1];

		}
		public override string ToString()
		{
			return $"{ID} - {Name}";

		}
	}
}