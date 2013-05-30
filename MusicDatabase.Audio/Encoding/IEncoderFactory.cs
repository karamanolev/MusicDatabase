namespace MusicDatabase.Audio.Encoding
{
    public interface IEncoderFactory
    {
        int ThreadCount { get; }
        IEncoder CreateEncoder(int threadNumber, IParallelTask task);
        void TryDeleteResult(IParallelTask task);
    }
}
