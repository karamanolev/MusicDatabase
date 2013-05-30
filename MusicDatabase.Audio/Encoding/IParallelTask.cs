using System;
using System.Linq;

namespace MusicDatabase.Audio.Encoding
{
    public interface IParallelTask
    {
        IEncoderFactory EncoderFactory { get; }
        string Target { get; }
        string Info { get; }
        EncodeTaskStatus Status { get; set; }
        double Progress { get; set; }
        event EventHandler ProgressChanged;
    }
}
