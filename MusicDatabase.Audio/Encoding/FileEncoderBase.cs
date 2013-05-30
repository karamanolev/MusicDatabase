using System;
using System.IO;
using CUETools.Codecs;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Tagging;
using NReplayGain;

namespace MusicDatabase.Audio.Encoding
{
    public abstract class FileEncoderBase : IEncoder
    {
        public const int BufferSize = 22050;

        private string targetFilename;
        private IAudioSource audioSource;
        private AudioFileTag tags;
        private TrackGain trackGain;
        private DrMeter drMeter;

        protected IAudioDest AudioDest { get; set; }

        public FileEncoderBase(IAudioSource audioSource, string targetFilename, AudioFileTag tags, TrackGain trackGain, DrMeter drMeter)
        {
            if (audioSource == null)
            {
                throw new SkipEncodingItemException("Unsupported audio source.");
            }

            this.targetFilename = targetFilename;
            this.audioSource = audioSource;
            Directory.CreateDirectory(Path.GetDirectoryName(this.targetFilename));
            this.tags = tags;
            this.trackGain = trackGain;
            this.drMeter = drMeter;
        }

        public void Encode()
        {
            AudioBuffer buffer = new AudioBuffer(audioSource.PCM, BufferSize);

            this.AudioDest.FinalSampleCount = this.audioSource.Length;

            while (audioSource.Read(buffer, BufferSize) > 0)
            {
                if (this.trackGain != null)
                {
                    DspHelper.AnalyzeSamples(this.trackGain, buffer);
                }
                if (this.drMeter != null)
                {
                    this.drMeter.Feed(buffer.Samples, buffer.Length);
                }

                this.AudioDest.Write(buffer);

                ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs((double)this.audioSource.Position / this.audioSource.Length);
                this.OnProgressChanged(eventArgs);
                if (eventArgs.Cancel)
                {
                    this.AudioDest.Close();
                    this.AudioDest = null;
                    Utility.TryDeleteFile(this.targetFilename);
                    return;
                }
            }

            this.AudioDest.Close();
            this.AudioDest = null;

            if (this.tags != null)
            {
                this.tags.WriteToFile(this.targetFilename);
            }
        }

        public void Dispose()
        {
            try
            {
                if (this.audioSource != null)
                {
                    this.audioSource.Close();
                    this.audioSource = null;
                }
            }
            catch { }

            try
            {
                if (this.AudioDest != null)
                {
                    this.AudioDest.Close();
                    this.AudioDest = null;
                }
            }
            catch { }
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
