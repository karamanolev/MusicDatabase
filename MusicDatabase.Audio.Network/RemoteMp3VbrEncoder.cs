using System.Net;
using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine.Tagging;
using NReplayGain;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Network
{
    public class RemoteMp3VbrEncoder : FileEncoderBase
    {
        public RemoteMp3VbrEncoder(IPAddress remoteAddress, IAudioSource audioSource, string targetFilename, AudioFileTag tags, int vbrQuality, TrackGain trackGain, DrMeter drMeter)
            : base(audioSource, targetFilename, tags, trackGain, drMeter)
        {
            this.AudioDest = new RemoteMp3VbrWriter(remoteAddress, targetFilename, audioSource.PCM)
            {
                CompressionLevel = vbrQuality
            };
        }
    }
}
