using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RumbleModdingAPI.RMAPI;
using UnityEngine;
using UnityEngine.UI;
using static ObsAutoRecorder.DebugHelper;

namespace ObsAutoRecorder.InputManager;

internal class AudioManager
{
    
    internal static AudioCall Confirmation;

    static string InputConfirm => "inputConfirm.wav";
    static string AudioAssets => "AudioAssets";
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
        ObsAutoRecorder.Instance.AudioVolume.OnPreferenceValueChanged += UpdateVolumes
    }

    internal void UpdateVolumes(float oldVal, float newVal)
    {
        AudioCall.GeneralAudioSettings generalAudioSettings = new AudioCall.GeneralAudioSettings();
        generalAudioSettings.SetVolume(SFXvolume);
        generalAudioSettings.Pitch = 1f;
        if (null!=Confirmation) Confirmation.generalSettings = generalAudioSettings;
    }

    static void PopulateMissingAudio()
    {
        string SfxDir => Path.Combine(ObsAutoRecorder.USER_DATA, AudioAssets);

        Directory.CreateDirectory(SfxDir);

        files[] = Directory.GetFiles(SfxDir).Select(Path.GetFileName).ToArray();

        if(files.Contains(InputConfirm))
        {
            var assembly = typeof(ObsAutoRecorder).Assembly;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(r => r.StartsWith($"ObsAutoRecorder.Assets.{AudioAssets}.", StringComparison.OrdinalIgnoreCase));

            foreach (var resourceName in resourceNames)
            {
                string fileName = resourceName.Substring($"ObsAutoRecorder.Assets.{folderName}.".Length);
                string outPath = Path.Combine(SfxDir, fileName);

                using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
                using (var fileStream = File.Create(outPath))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }
        }


    }
}