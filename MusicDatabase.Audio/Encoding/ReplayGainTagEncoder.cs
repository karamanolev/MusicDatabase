using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Tagging;
using NReplayGain;

namespace MusicDatabase.Audio.Encoding
{
    class ReplayGainTagEncoder : IEncoder
    {
        private List<ReplayGainTask.Item> trackTasks;
        private ReplayGainTask replayGainTask;

        public ReplayGainTagEncoder(ReplayGainTask task)
        {
            this.trackTasks = task.TrackTasks;
            this.replayGainTask = task;
        }

        public void Encode()
        {
            while (true)
            {
                if (trackTasks.Any(t => t.Task.Status == EncodeTaskStatus.Cancelled))
                {
                    return;
                }
                else if (trackTasks.Any(t => t.Task.Status == EncodeTaskStatus.Faulted || t.Task.Status == EncodeTaskStatus.FaultedWaiting))
                {
                    throw new Exception("Track faulted");
                }
                else if (trackTasks.Any(t => t.Task.Status == EncodeTaskStatus.Skipped))
                {
                    throw new SkipEncodingItemException("");
                }
                else if (trackTasks.All(t => t.Task.Status == EncodeTaskStatus.Completed))
                {
                    this.ComputeReplayGain();
                    return;
                }

                Thread.Sleep(100);

                ProgressChangedEventArgs e = new ProgressChangedEventArgs(0);
                if (e.Cancel)
                {
                    return;
                }
            }
        }

        private void ComputeReplayGain()
        {
            this.OnProgressChanged(new ProgressChangedEventArgs(0));

            if (this.replayGainTask.RecalculateReplayGain)
            {
                this.replayGainTask.AlbumGain = new AlbumGain();
                var albumGainData = this.replayGainTask.AlbumGain;

                for (int i = 0; i < trackTasks.Count; ++i)
                {
                    FileEncodeTask task = this.trackTasks[i].Task;
                    this.trackTasks[i].Track.ReplayGainTrackGain = task.TrackGain.GetGain();
                    this.trackTasks[i].Track.ReplayGainTrackPeak = task.TrackGain.GetPeak();
                    albumGainData.AppendTrackData(task.TrackGain);

                    this.OnProgressChanged(new ProgressChangedEventArgs((double)(i + 1) / trackTasks.Count / 2));
                }

                this.replayGainTask.Release.ReplayGainAlbumGain = albumGainData.GetGain();
                this.replayGainTask.Release.ReplayGainAlbumPeak = albumGainData.GetPeak();
            }

            if (this.replayGainTask.SaveDynamicRange)
            {
                foreach (ReplayGainTask.Item item in this.trackTasks)
                {
                    item.Track.DynamicRange = item.Task.DrMeter.GetDynamicRange();
                }
            }

            for (int i = 0; i < trackTasks.Count; ++i)
            {
                FileEncodeTask task = this.trackTasks[i].Task;
                Track track = this.trackTasks[i].Track;

                double trackGain = track.ReplayGainTrackGain;
                double trackPeak = this.replayGainTask.WritePeaks ? track.ReplayGainTrackPeak : double.NaN;
                double albumGain = this.replayGainTask.Release.ReplayGainAlbumGain;
                double albumPeak = this.replayGainTask.WritePeaks ? this.replayGainTask.Release.ReplayGainAlbumPeak : double.NaN;
                AudioFileTag.WriteReplayGainData(task.TargetFilename, trackGain, trackPeak, albumGain, albumPeak, true);

                this.OnProgressChanged(new ProgressChangedEventArgs(0.5 + (double)(i + 1) / trackTasks.Count / 2));
            }

            for (int i = 0; i < trackTasks.Count; ++i)
            {
                if (this.trackTasks[i].Task.TrackGain != null)
                {
                    this.trackTasks[i].Task.TrackGain.Dispose();
                }
            }
        }

        public void Dispose()
        {
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
