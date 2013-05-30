using System;
using System.Linq;
using CUETools.Codecs;
using NReplayGain;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio
{
    public class DspHelper
    {
        public static void AnalyzeSamples(TrackGain trackGain, AudioBuffer buffer)
        {
            int[] leftSamples = new int[buffer.Length];
            int[] rightSamples = new int[buffer.Length];
            for (int j = 0; j < buffer.Length; ++j)
            {
                leftSamples[j] = buffer.Samples[j, 0];
                rightSamples[j] = buffer.Samples[j, 1];
            }
            trackGain.AnalyzeSamples(leftSamples, rightSamples);
        }

        public static TrackGain CreateTrackGain(IAudioSource audioSource)
        {
            if (audioSource != null && ReplayGain.IsSupportedFormat(audioSource.PCM.SampleRate, audioSource.PCM.BitsPerSample))
            {
                return new TrackGain(audioSource.PCM.SampleRate, audioSource.PCM.BitsPerSample);
            }
            return null;
        }

        public static DrMeter CreateDrMeter(IAudioSource audioSource)
        {
            return new DrMeter(audioSource.PCM.ChannelCount, audioSource.PCM.SampleRate, audioSource.PCM.BitsPerSample);
        }

        public static int GetRoundedDr(double dr)
        {
            return (int)Math.Round(dr);
        }
    }
}
