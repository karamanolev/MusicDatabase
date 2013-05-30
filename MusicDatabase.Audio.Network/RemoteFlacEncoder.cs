using System.Net;
using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine.Tagging;
using NReplayGain;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Network
{
    public class RemoteFlacEncoder : FileEncoderBase
    {
        public RemoteFlacEncoder(IPAddress remoteAddress, IAudioSource audioSource, string targetFilename, AudioFileTag tags, int compressionLevel, TrackGain trackGain, DrMeter drMeter)
            : base(audioSource, targetFilename, tags, trackGain, drMeter)
        {
            this.AudioDest = new RemoteFlacWriter(remoteAddress, targetFilename, audioSource.PCM)
            {
                CompressionLevel = compressionLevel
            };
        }
    }
}
