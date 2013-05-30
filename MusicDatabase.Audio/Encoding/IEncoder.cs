using System;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Encoding
{
    public interface IEncoder : IDisposable
    {
        void Encode();
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
    }
}
