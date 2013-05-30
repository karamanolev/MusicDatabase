namespace MusicDatabase.Audio.Encoding
{
    public enum EncodeTaskStatus
    {
        Waiting,
        Processing,
        Completed,
        Cancelled,
        Faulted,
        FaultedWaiting,
        Skipped
    }
}
