using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using RumbleModdingAPI.RMAPI;
using static RumbleModdingAPI.RMAPI.AudioManager;
using Il2CppRUMBLE.Audio;
using UnityEngine;
using UnityEngine.UI;
using static ObsAutoRecorder.DebugHelper;

namespace ObsAutoRecorder.InputManager;

internal class SfxManager
{

	internal static AudioCall Confirmation;

	static string InputConfirm => "inputConfirm.wav";
	static string SfxDir => "SFXAssets";
	static float VolumeSetting => ObsAutoRecorder.Instance.AudioVolume.Value;

	internal static void Init()
	{
		PopulateMissingAudio();
		try
		{
			Confirmation = CreateAudioCall(Path.Combine(SfxDir, InputConfirm), VolumeSetting);
		}
		catch (Exception e)
		{
			Error($"Error initializing audio calls: \n{e}");
		}
		ObsAutoRecorder.Instance.AudioVolume.OnEntryValueChanged.Subscribe(UpdateVolumes);
	}

	internal static void UpdateVolumes(float oldVal, float newVal)
	{
		AudioCall.GeneralAudioSettings generalAudioSettings = new AudioCall.GeneralAudioSettings();
		generalAudioSettings.SetVolume(VolumeSetting);
		generalAudioSettings.Pitch = 1f;
		if (null != Confirmation) Confirmation.generalSettings = generalAudioSettings;
	}

	static void PopulateMissingAudio()
	{
		try
		{


			string sfxFolderCandidate = Path.Combine(ObsAutoRecorder.USER_DATA, SfxManager.SfxDir);

			Deb($"PopulateMissingAudio - sfxFolderCandidate: {sfxFolderCandidate}");
			Directory.CreateDirectory(sfxFolderCandidate);

			string[] files = Directory.GetFiles(sfxFolderCandidate).Select(Path.GetFileName).ToArray();

			if (!files.Contains(InputConfirm))
			{
				Log($"SfxManager: No Sfx files found. Extracting...");
				var assembly = typeof(ObsAutoRecorder).Assembly;
				var resourceNames = assembly.GetManifestResourceNames()
					.Where(r => r.StartsWith($"ObsAutoRecorder.Assets.{SfxManager.SfxDir}.", StringComparison.OrdinalIgnoreCase));

				foreach (var resourceName in resourceNames)
				{
					Deb($"PopulateMissingAudio Resource Name: { resourceName}");
					string fileName = resourceName.Substring($"ObsAutoRecorder.Assets.{SfxDir}.".Length);
					string outPath = Path.Combine(sfxFolderCandidate, fileName);

					using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
					using (var fileStream = File.Create(outPath))
					{
						resourceStream.CopyTo(fileStream);
					}
					Log($"Successfully extracted {fileName}");
				}
			}

		}catch(Exception e)
		{
			Error($"PopulateMissingAudio():\n{e.ToString()}");
		}
	}
}