using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MusicDatabase.Audio;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Settings;

namespace MusicDatabase.Helpers
{
    class ReplayGainUpdateHelper : HelperBase
    {
        private SettingsManager settingsManager;

        public ReplayGainUpdateHelper(MusicDatabaseWindow parentWindow, SettingsManager settingsManager, ICollectionSessionFactory collectionSessionFactory)
            : base(parentWindow, collectionSessionFactory)
        {
            this.settingsManager = settingsManager;
        }

        public void RunOneRelease(Release release)
        {
            this.RunInternal(release);
        }

        public void RunAllReleases()
        {
            this.RunInternal(this.CollectionManager.Releases.ToArray());
        }

        private void RunInternal(params Release[] releases)
        {
            List<IParallelTask> tasks = null;

            Progress<double> progress = new Progress<double>();
            new WaitWindow("Generating tasks...").ShowDialog(this.ParentWindow, new Task(() =>
            {
                tasks = this.GenerateReplayGainTasks(releases, progress);
            }), progress);

            if (tasks != null)
            {
                EncoderController encoderController = new EncoderController(tasks.ToArray(), this.settingsManager.Settings.ActualLocalConcurrencyLevel);
                EncodingWindow encodingWindow = new EncodingWindow(encoderController);
                if (encodingWindow.ShowDialog(this.ParentWindow) == true)
                {
                    foreach (Release item in releases)
                    {
                        this.CollectionManager.Save(item);
                    }
                }
            }
        }

        private List<IParallelTask> GenerateReplayGainTasks(Release[] releases, IProgress<double> progress)
        {
            List<IParallelTask> tasks = new List<IParallelTask>();

            DspEncoderFactory replayGainFactory = new DspEncoderFactory(this.settingsManager.Settings.LocalConcurrencyLevel, true, false);

            double progressCoef = 1.0 / releases.Length;
            int processed = 0;
            foreach (Release release in releases)
            {
                ReplayGainTask rgTask = new ReplayGainTask(replayGainFactory, release, true, true, false);

                foreach (Track track in release.Tracklist)
                {
                    string filename = Path.Combine(this.CollectionManager.Settings.MusicDirectory, track.RelativeFilename);
                    var task = new FileEncodeTask(replayGainFactory, () => AudioHelper.GetAudioSourceForFile(filename), filename, null);

                    tasks.Add(task);
                    rgTask.AddItem(track, task);
                }

                tasks.Add(rgTask);

                ++processed;
                progress.Report(processed * progressCoef);
            }

            return tasks;
        }
    }
}
