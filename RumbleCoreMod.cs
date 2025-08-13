using MelonLoader;
using RumbleModdingAPI;
using UnityEngine;


[assembly: MelonInfo(typeof(ObsAutoRecorder.ObsAutoRecorder), ObsAutoRecorder.BuildInfo.Name, ObsAutoRecorder.BuildInfo.Version, ObsAutoRecorder.BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

namespace ObsAutoRecorder
{
    public static class BuildInfo
    {
        public const string Name = "ObsAutoRecorder";
        public const string Author = "iListen2Sound";
        public const string Version = "1.0.0";
    }

    public class ObsAutoRecorder : MelonMod
    {
        private static bool debugMode = true;

        public override void OnLateInitializeMelon()
        {
            Calls.onMapInitialized += OnMapInitialized;
        }
        /// <summary>
        /// Called when map is fully initialized reducing the risk of null references.
        /// </summary>
        private void OnMapInitialized()
        {

        }

        public override void OnFixedUpdate()
        {

        }

        /// <summary>
        /// Logs a message to the console
        /// </summary>
        /// <param name="message"></param>
        /// <param name="debugOnly"></param>
        private void Log(string message, bool debugOnly = false)
        {
            if (!debugOnly)
            {
                LoggerInstance.Msg(message);
                return;
            }
            if (debugMode)
                LoggerInstance.Msg(message);
        }

    }
}
