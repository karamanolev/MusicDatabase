using System;
using System.Linq;
using MusicDatabase.Engine;
using System.Threading;
using NReplayGain;
using MusicDatabase.Engine.Tagging;

namespace MusicDatabase.Audio.Encoding
{
    class ReplayGainTagEncoder : IEncoder
    {
        private FileEncodeTask[] trackTasks;
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
                if (trackTasks.Any(t => t.Status == EncodeTaskStatus.Cancelled))
                {
                    return;
                }
                else if (trackTasks.Any(t => t.Status == EncodeTaskStatus.Faulted || t.Status == EncodeTaskStatus.FaultedWaiting))
                {
                    throw new Exception("Track faulted");
                }
                else if (trackTasks.Any(t => t.Status == EncodeTaskStatus.Skipped))
                {
                    throw new SkipEncodingItemException("");
                }
                else if (trackTasks.All(t => t.Status == EncodeTaskStatus.Completed))
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

            this.replayGainTask.AlbumGain = new AlbumGain();
            var albumGainData = this.replayGainTask.AlbumGain;

            for (int i = 0; i < trackTasks.Length; ++i)
            {
                FileEncodeTask task = this.trackTasks[i];
                albumGainData.AppendTrackData(task.TrackGain);

                this.OnProgressChanged(new ProgressChangedEventArgs((double)(i + 1) / trackTasks.Length / 2));
            }

            for (int i = 0; i < trackTasks.Length; ++i)
            {
                FileEncodeTask task = this.trackTasks[i];
                double trackGain = task.TrackGain.GetGain();
                double trackPeak = this.replayGainTask.WritePeaks ? task.TrackGain.GetPeak() : double.NaN;
                double albumGain = albumGainData.GetGain();
                double albumPeak = this.replayGainTask.WritePeaks ? albumGainData.GetPeak() : double.NaN;
                AudioFileTag.WriteReplayGainData(task.TargetFilename, trackGain, trackPeak, albumGain, albumPeak, true);

                this.OnProgressChanged(new ProgressChangedEventArgs(0.5 + (double)(i + 1) / trackTasks.Length / 2));
            }

            for (int i = 0; i < trackTasks.Length; ++i)
            {
                this.trackTasks[i].TrackGain.Dispose();
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
