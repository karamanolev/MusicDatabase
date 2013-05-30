using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Tagging;
using NReplayGain;

namespace MusicDatabase.Audio.Mp3
{
    public class LocalMp3Encoder : FileEncoderBase
    {
        public LocalMp3Encoder(IAudioSource audioSource, string targetFilename, AudioFileTag tags, int vbrQuality, TrackGain trackGain, DrMeter drMeter)
            : base(audioSource, targetFilename, tags, trackGain, drMeter)
        {
            this.AudioDest = new LameWriter(targetFilename, audioSource.PCM)
            {
                Settings = LameWriterSettings.CreateVbr(vbrQuality)
            };
        }
    }
}
