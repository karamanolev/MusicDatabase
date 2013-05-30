using System;

namespace MusicDatabase.Audio.Encoding
{
    public interface IEncoderController
    {
        IParallelTask[] Tasks { get; }
        EncoderControllerStatus Status { get; }

        void Start();
        void Cancel();

        event EventHandler Completed;
    }
}
