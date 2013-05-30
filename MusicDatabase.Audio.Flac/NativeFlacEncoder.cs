using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Tagging;
using NReplayGain;

namespace MusicDatabase.Audio.Flac
{
    public class NativeFlacEncoder : FileEncoderBase
    {
        public NativeFlacEncoder(IAudioSource audioSource, string targetFilename, AudioFileTag tags, int compressionLevel, TrackGain trackGain, DrMeter drMeter)
            : base(audioSource, targetFilename, tags, trackGain, drMeter)
        {
            this.AudioDest = new NativeFlacWriter(targetFilename, audioSource.PCM)
            {
                CompressionLevel = compressionLevel
            };
        }
    }
}
