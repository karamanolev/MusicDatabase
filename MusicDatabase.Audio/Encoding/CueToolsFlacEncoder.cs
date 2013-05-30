using CUETools.Codecs;
using CUETools.Codecs.FLAC;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Tagging;
using NReplayGain;

namespace MusicDatabase.Audio.Encoding
{
    public class CueToolsFlacEncoder : FileEncoderBase
    {
        public CueToolsFlacEncoder(IAudioSource audioSource, string targetFilename, AudioFileTag tags, int compressionLevel, TrackGain trackGain, DrMeter drMeter)
            : base(audioSource, targetFilename, tags, trackGain, drMeter)
        {
            this.AudioDest = new FLACWriter(targetFilename, audioSource.PCM)
            {
                CompressionLevel = compressionLevel
            };
        }
    }
}
