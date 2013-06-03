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
    class DynamicRangeUpdater : HelperBase
    {
        Dictionary<Track, FileEncodeTask> trackToTask;
        List<FileEncodeTask> tasks;
        private SettingsManager settingsManager;

        public DynamicRangeUpdater(MusicDatabaseWindow parentWindow, SettingsManager settingsManager, ICollectionSessionFactory collectionSessionFactory)
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
            this.trackToTask = new Dictionary<Track, FileEncodeTask>();
            this.tasks = new List<FileEncodeTask>();

            Progress<double> progress = new Progress<double>();
            new WaitWindow("Generating tasks...").ShowDialog(this.ParentWindow, new Task(() =>
            {
                this.GenerateDynamicRangeTasks(releases, progress);
            }), progress);

            EncoderController encoderController = new EncoderController(this.tasks.ToArray(), this.settingsManager.Settings.ActualLocalConcurrencyLevel);

            EncodingWindow encodingWindow = new EncodingWindow(encoderController);
            if (encodingWindow.ShowDialog(this.ParentWindow) == true)
            {
                foreach (KeyValuePair<Track, FileEncodeTask> items in trackToTask)
                {
                    if (items.Value.DrMeter != null)
                    {
                        items.Key.DynamicRange = items.Value.DrMeter.GetDynamicRange();
                    }
                }

                foreach (Release release in releases)
                {
                    release.UpdateDynamicProperties();
                    this.CollectionManager.Save(release);
                }

                CollectionManagerGlobal.OnCollectionChanged();
            }
        }

        private void GenerateDynamicRangeTasks(Release[] releases, IProgress<double> progress)
        {
            DspEncoderFactory replayGainFactory = new DspEncoderFactory(this.settingsManager.Settings.LocalConcurrencyLevel, false, true);

            double progressCoef = 1.0 / releases.Length;
            int processed = 0;
            foreach (Release release in releases)
            {
                foreach (Track track in release.Tracklist)
                {
                    string filename = Path.Combine(this.CollectionManager.Settings.MusicDirectory, track.RelativeFilename);
                    var task = new FileEncodeTask(replayGainFactory, () => AudioHelper.GetAudioSourceForFile(filename), filename, null);

                    this.trackToTask[track] = task;
                    this.tasks.Add(task);
                }

                ++processed;
                progress.Report(processed * progressCoef);
            }
        }
    }
}
