namespace ObsAutoRecorder.Rewrite;

internal class Recording
{

    internal static Queue<Recording> RecordingQueue = new();

    internal string OriginalOutputPath {get; set;}
    internal string TargetOutputPath {get; set;}

    internal PlayFabinfo PlayerInfo {get; set;}

    internal void StartRecording()
    {

    }

    internal void StopRecording()
    {

    }
}