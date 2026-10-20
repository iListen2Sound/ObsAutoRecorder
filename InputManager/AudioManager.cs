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
    static string InputConfirm => "inputConfirm.wav";
    static string AudioAssets => "AudioAssets"
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