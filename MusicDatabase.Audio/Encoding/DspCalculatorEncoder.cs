using System;
using System.Linq;
using CUETools.Codecs;
using MusicDatabase.Engine;
using NReplayGain;

namespace MusicDatabase.Audio.Encoding
{
    class DspCalculatorEncoder : IEncoder
    {
        private IAudioSource audioSource;
        private TrackGain trackGain;
        private DrMeter drMeter;

        public DspCalculatorEncoder(IAudioSource audioSource, TrackGain trackGain, DrMeter drMeter)
        {
            this.audioSource = audioSource;
            this.trackGain = trackGain;
            this.drMeter = drMeter;
        }

        public void Encode()
        {
            if (this.trackGain == null && this.drMeter == null)
            {
                throw new SkipEncodingItemException("Neither ReplayGain nor DynamicRange to calculate.");
            }

            AudioBuffer buffer = new AudioBuffer(audioSource.PCM, FileEncoderBase.BufferSize);

            while (audioSource.Read(buffer, FileEncoderBase.BufferSize) > 0)
            {
                if (this.trackGain != null)
                {
                    DspHelper.AnalyzeSamples(this.trackGain, buffer);
                }
                if (this.drMeter != null)
                {
                    this.drMeter.Feed(buffer.Samples, buffer.Length);
                }

                ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs((double)this.audioSource.Position / this.audioSource.Length);
                this.OnProgressChanged(eventArgs);
                if (eventArgs.Cancel)
                {
                    this.trackGain = null;
                    this.drMeter = null;
                    return;
                }
            }

            if (this.drMeter != null)
            {
                this.drMeter.Finish();
            }
        }

        public void Dispose()
        {
            if (this.audioSource != null)
            {
                this.audioSource.Close();
                this.audioSource = null;
            }
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        private void OnProgressChanged(ProgressChangedEventArgs eventArgs)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, eventArgs);
            }
        }
    }
}
