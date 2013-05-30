using System;
using System.IO;
using CUETools.Codecs;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Tagging;
using NReplayGain;

namespace MusicDatabase.Audio.Encoding
{
    public class FileEncodeTask : IParallelTask
    {
        private EncodeTaskStatus status = EncodeTaskStatus.Waiting;
        private double progress = 0;

        public Func<IAudioSource> AudioSourceLazy { get; private set; }
        public string TargetFilename { get; private set; }
        public AudioFileTag Tag { get; private set; }
        public TrackGain TrackGain { get; set; }
        public DrMeter DrMeter { get; set; }

        public IEncoderFactory EncoderFactory { get; private set; }

        public string Target
        {
            get
            {
                return Path.GetFileName(this.TargetFilename);
            }
        }

        public string Info
        {
            get
            {
                if (this.Tag == null)
                {
                    return this.TargetFilename;
                }
                else
                {
                    return Tag.AlbumArtists + " - " + Tag.Title;
                }
            }
        }

        public EncodeTaskStatus Status
        {
            get
            {
                return this.status;
            }
            set
            {
                if (this.status != value)
                {
                    this.status = value;
                    this.OnProgressChanged();
                }
            }
        }

        public double Progress
        {
            get
            {
                return this.progress;
            }
            set
            {
                if (this.progress != value)
                {
                    this.progress = value;
                    this.OnProgressChanged();
                }
            }
        }

        public FileEncodeTask(IEncoderFactory encoderFactory, Func<IAudioSource> audioSourceLazy, string targetFilename, AudioFileTag tag)
        {
            this.EncoderFactory = encoderFactory;
            this.AudioSourceLazy = audioSourceLazy;
            this.TargetFilename = targetFilename;
            this.Tag = tag;
        }

        public event EventHandler ProgressChanged;
        private void OnProgressChanged()
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, EventArgs.Empty);
            }
        }
    }
}
