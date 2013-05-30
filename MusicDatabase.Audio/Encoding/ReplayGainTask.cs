using NReplayGain;
using System;
using System.IO;
using System.Linq;

namespace MusicDatabase.Audio.Encoding
{
    public class ReplayGainTask : IParallelTask
    {
        private double progress;

        public IEncoderFactory EncoderFactory { get; private set; }

        public string Target
        {
            get
            {
                if (this.TrackTasks[0].Tag == null)
                {
                    return Path.GetDirectoryName(this.TrackTasks[0].TargetFilename);
                }
                else
                {
                    return this.TrackTasks[0].Tag.Album;
                }
            }
        }

        public string Info
        {
            get
            {
                if (this.TrackTasks[0].Tag == null)
                {
                    return Path.GetDirectoryName(this.TrackTasks[0].TargetFilename);
                }
                else
                {
                    return this.TrackTasks[0].Tag.AlbumArtists + " - " + this.TrackTasks[0].Tag.Album;
                }
            }
        }

        public EncodeTaskStatus Status { get; set; }

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

        public FileEncodeTask[] TrackTasks { get; private set; }
        public bool WritePeaks { get; private set; }
        public AlbumGain AlbumGain { get; set; }

        public ReplayGainTask(IEncoderFactory factory, FileEncodeTask[] trackTasks, bool writePeaks)
        {
            this.EncoderFactory = factory;
            this.TrackTasks = trackTasks;
            this.WritePeaks = writePeaks;
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
