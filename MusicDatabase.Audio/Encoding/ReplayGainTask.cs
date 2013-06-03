using MusicDatabase.Engine.Entities;
using NReplayGain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusicDatabase.Audio.Encoding
{
    public class ReplayGainTask : IParallelTask
    {
        internal class Item
        {
            public Track Track { get; private set; }
            public FileEncodeTask Task { get; private set; }

            public Item(Track track, FileEncodeTask task)
            {
                this.Track = track;
                this.Task = task;
            }
        }

        private double progress;

        public IEncoderFactory EncoderFactory { get; private set; }

        public string Target
        {
            get
            {
                if (this.TrackTasks[0].Task.Tag == null)
                {
                    return Path.GetDirectoryName(this.TrackTasks[0].Task.TargetFilename);
                }
                else
                {
                    return this.TrackTasks[0].Task.Tag.Album;
                }
            }
        }

        public string Info
        {
            get
            {
                if (this.TrackTasks[0].Task.Tag == null)
                {
                    return Path.GetDirectoryName(this.TrackTasks[0].Task.TargetFilename) + " ReplayGain";
                }
                else
                {
                    return this.TrackTasks[0].Task.Tag.AlbumArtists + " - " + this.TrackTasks[0].Task.Tag.Album + " ReplayGain";
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

        internal List<Item> TrackTasks { get; private set; }

        public bool RecalculateReplayGain { get; private set; }
        public Release Release { get; private set; }
        public bool WritePeaks { get; private set; }
        public bool SaveDynamicRange { get; private set; }
        public AlbumGain AlbumGain { get; set; }

        public ReplayGainTask(IEncoderFactory factory, Release release, bool recalculateReplayGain, bool writePeaks, bool saveDynamicRange)
        {
            this.EncoderFactory = factory;
            this.Release = release;
            this.TrackTasks = new List<Item>();
            this.RecalculateReplayGain = recalculateReplayGain;
            this.WritePeaks = writePeaks;
            this.SaveDynamicRange = saveDynamicRange;
        }

        public void AddItem(Track track, FileEncodeTask task)
        {
            this.TrackTasks.Add(new Item(track, task));
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
