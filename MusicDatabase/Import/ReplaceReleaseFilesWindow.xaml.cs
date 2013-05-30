using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using MusicDatabase.Audio;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Audio.Flac;
using MusicDatabase.Audio.Network;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Tagging;

namespace MusicDatabase.Import
{
    /// <summary>
    /// Interaction logic for ReplaceReleaseFilesWindow.xaml
    /// </summary>
    public partial class ReplaceReleaseFilesWindow : MusicDatabaseWindow
    {
        private Release release;
        private ObservableCollection<IImportSourceItem> items;

        public ReplaceReleaseFilesWindow(ICollectionSessionFactory collectionSessionFactory, int releaseId)
            : base(collectionSessionFactory)
        {
            this.release = this.CollectionManager.Releases.Where(r => r.Id == releaseId).First();

            InitializeComponent();

            this.networkBox.CollectionManager = this.CollectionManager;
            this.networkBox.SettingsManager = this.SettingsManager;

            this.items = new ObservableCollection<IImportSourceItem>();
            this.items.CollectionChanged += this.items_CollectionChanged;

            this.listTracks.ItemsSource = release.Tracklist;
            this.listImportItems.ItemsSource = this.items;
        }

        void items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.okCancelBox.IsOKEnabled = this.items.Count == release.Tracklist.Count;
        }

        private void ListView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void ListView_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            string[] droppedItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.AddItems(droppedItems);
        }

        public void AddItems(string[] items)
        {
            List<string> files = new List<string>();

            Action<string> addDirectory = null;
            addDirectory = directory =>
            {
                foreach (string subDirectory in Directory.GetDirectories(directory))
                {
                    addDirectory(subDirectory);
                }
                foreach (string file in Directory.GetFiles(directory))
                {
                    files.Add(file);
                }
            };

            foreach (string item in items)
            {
                if (File.Exists(item))
                {
                    files.Add(item);
                }
                else if (Directory.Exists(item))
                {
                    addDirectory(item);
                }
            }

            files.Sort();

            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".cue")
                {
                    this.AddCue(file);
                }
                else if (AudioHelper.IsSupportedAudioSource(file))
                {
                    this.AddAudioFile(file);
                }
            }
        }

        private void AddCue(string file)
        {
            CueSheet sheet;
            try
            {
                sheet = new CueSheet(file);
            }
            catch (Exception e)
            {
                Utility.WriteToErrorLog(e.ToString());
                Dialogs.Error("Error reading cue file. Invalid format.");
                return;
            }

            bool isOriginal;
            string targetFilename = sheet.DiscoverTarget(out isOriginal);

            if (targetFilename == null)
            {
                Dialogs.Error("Cue target was not found!");
                return;
            }
            else if (!isOriginal)
            {
                Dialogs.Inform("Cue target was not found, but successfully discovered.");
            }

            if (!AudioHelper.IsSupportedAudioSource(targetFilename))
            {
                Dialogs.Error("Unsupported audio source!");
                return;
            }

            for (int i = 0; i < sheet.Tracks.Length; ++i)
            {
                CueSourceItem item = new CueSourceItem(targetFilename, sheet, i);
                this.AddItem(item);
            }
        }

        private void AddAudioFile(string file)
        {
            FileSourceItem item = new FileSourceItem(file);
            this.AddItem(item);
        }

        private void AddItem(IImportSourceItem importSourceItem)
        {
            this.items.Add(importSourceItem);
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            using (var transaction = this.CollectionManager.BeginTransaction())
            {
                this.release.DateAudioModified = DateTime.Now;
                transaction.Commit();
            }

            IEncoderFactory encoderFactory;
            if (this.CollectionManager.Settings.NetworkEncoding)
            {
                encoderFactory = new RemoteFlacEncoderFactory(this.networkBox.Servers, 8, this.SettingsManager.Settings.ActualLocalConcurrencyLevel, true);
            }
            else
            {
                encoderFactory = new NativeFlacEncoderFactory(8, this.SettingsManager.Settings.ActualLocalConcurrencyLevel, true);
            }

            IEncoderFactory replayGainFactory = new DspEncoderFactory(this.SettingsManager.Settings.ActualLocalConcurrencyLevel, true, true);

            List<IParallelTask> tasks = new List<IParallelTask>();
            Dictionary<Track, FileEncodeTask> trackToTask = new Dictionary<Track, FileEncodeTask>();
            for (int i = 0; i < this.items.Count; ++i)
            {
                Track track = this.release.Tracklist[i];
                IImportSourceItem importSourceItem = this.items[i];

                track.RelativeFilename = FilenameGenerator.PatternToFilename(
                        this.CollectionManager.Settings.FileNamingPattern,
                        this.release, track) + ".flac";

                AudioFileTag tag = new AudioFileTag(this.release, track);

                string filename = Path.Combine(this.CollectionManager.Settings.MusicDirectory, track.RelativeFilename);

                var task = new FileEncodeTask(
                    encoderFactory,
                    () => importSourceItem.GetAudioSource(),
                    filename,
                    tag
                    );
                tasks.Add(task);
                trackToTask[track] = task;
            }

            ReplayGainTask rgTask = new ReplayGainTask(replayGainFactory, tasks.Cast<FileEncodeTask>().ToArray(), true);
            tasks.Add(rgTask);

            int concurrency = Math.Max(encoderFactory.ThreadCount, replayGainFactory.ThreadCount);
            EncoderController controller = new EncoderController(tasks.ToArray(), concurrency);
            EncodingWindow window = new EncodingWindow(controller);
            if (window.ShowDialog(this) == true)
            {
                using (var transaction = this.CollectionManager.BeginTransaction())
                {
                    foreach (KeyValuePair<Track, FileEncodeTask> trackTask in trackToTask)
                    {
                        trackTask.Key.DynamicRange = trackTask.Value.DrMeter.GetDynamicRange();
                        trackTask.Key.ReplayGainTrackGain = trackTask.Value.TrackGain.GetGain();
                        trackTask.Key.ReplayGainTrackPeak = trackTask.Value.TrackGain.GetPeak();
                    }
                    this.release.ReplayGainAlbumGain = rgTask.AlbumGain.GetGain();
                    this.release.ReplayGainAlbumPeak = rgTask.AlbumGain.GetPeak();

                    this.release.UpdateDynamicProperties();

                    transaction.Commit();
                }

                this.Close();
            }

            CollectionManager.OnCollectionChanged();
        }
    }
}
