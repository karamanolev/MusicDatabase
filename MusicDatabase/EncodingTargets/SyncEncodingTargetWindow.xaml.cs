using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MusicDatabase.Audio;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Audio.Mp3;
using MusicDatabase.Audio.Network;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Tagging;

namespace MusicDatabase.EncodingTargets
{
    public partial class SyncEncodingTargetWindow : MusicDatabaseWindow
    {
        public class TrackToEncodeListViewItem
        {
            public string Release { get; set; }
            public string Title { get; set; }
        }

        private EncodingTarget encodingTarget;
        private Task scanWrkerTask;
        private bool cancelScanning;
        private EncodingTargetScanResult scanResult;

        public SyncEncodingTargetWindow(ICollectionSessionFactory collectionSessionFactory, int encodingTargetId)
            : base(collectionSessionFactory)
        {
            this.encodingTarget = this.CollectionManager.Settings.EncodingTargets.Where(t => t.Id == encodingTargetId).FirstOrDefault();

            InitializeComponent();

            this.textLameVersion.Text = LameWriter.EncoderVersion;

            this.networkBox.CollectionManager = this.CollectionManager;
            this.networkBox.SettingsManager = this.SettingsManager;

            this.scanWrkerTask = new Task(this.WorkerTask);
            this.scanWrkerTask.Start();
        }

        private void WorkerTask()
        {
            EncodingTargetScanner scanner = new EncodingTargetScanner(this.CollectionManager, this.encodingTarget);
            scanner.ProgressChanged += ProgressBarUpdater.CreateHandler(this.Dispatcher, this.progressScan, () => this.cancelScanning);
            this.scanResult = scanner.Scan();

            if (!this.cancelScanning)
            {
                this.Dispatcher.InvokeAction(() =>
                {
                    this.groupTracksToEncode.Header = "Tracks To Encode (" + scanResult.ReleasesToEncodeTrackCount + ")";
                    this.groupFilesToDelete.Header = "Files To Delete (" + scanResult.FilesToDelete.Length + ")";

                    List<TrackToEncodeListViewItem> tracksToEncode = new List<TrackToEncodeListViewItem>();
                    foreach (var releaseToEncode in this.scanResult.ReleasesToEncode)
                    {
                        foreach (var trackToEncode in releaseToEncode.Tracklist)
                        {
                            tracksToEncode.Add(new TrackToEncodeListViewItem()
                            {
                                Release = releaseToEncode.JoinedAlbumArtists + " - " + releaseToEncode.Title,
                                Title = trackToEncode.Title
                            });
                        }
                    }
                    this.listTracksToEncode.ItemsSource = tracksToEncode;

                    this.listFilesToDelete.ItemsSource = this.scanResult.FilesToDelete.ToArray();

                    this.btnSync.IsEnabled =
                        this.scanResult.ReleasesToEncodeTrackCount > 0 ||
                        this.scanResult.FilesToDelete.Length > 0;
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.scanWrkerTask.IsCompleted)
            {
                e.Cancel = true;
                if (!this.cancelScanning)
                {
                    this.cancelScanning = true;
                    this.scanWrkerTask.ContinueWith(t =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.Close();
                        }));
                    });
                }
                return;
            }
        }

        private void btnSync_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            bool successfullyDeletedFiles = true;
            foreach (string fileToDelete in this.scanResult.FilesToDelete)
            {
                successfullyDeletedFiles = Utility.TryDeleteFile(fileToDelete) && successfullyDeletedFiles;
                successfullyDeletedFiles = Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(fileToDelete)) && successfullyDeletedFiles;
            }
            if (!successfullyDeletedFiles)
            {
                Dialogs.Inform("Some files or folders were not successfully deleted. Please unlock them and rerun the sync.");
            }

            if (this.scanResult.ReleasesToEncode.Count > 0)
            {
                int localConcurrencyLevel = this.SettingsManager.Settings.ActualLocalConcurrencyLevel;

                IEncoderFactory encoderFactory;
                if (this.CollectionManager.Settings.NetworkEncoding)
                {
                    encoderFactory = new RemoteMp3EncoderFactory(this.networkBox.Servers, this.encodingTarget.Mp3Settings.VbrQuality, localConcurrencyLevel, false);
                }
                else
                {
                    encoderFactory = new LocalMp3EncoderFactory(this.encodingTarget.Mp3Settings.VbrQuality, localConcurrencyLevel, false);
                }

                IEncoderFactory replayGainFactory = new DspEncoderFactory(this.SettingsManager.Settings.ActualLocalConcurrencyLevel, true, false);

                List<IParallelTask> tasks = new List<IParallelTask>();
                foreach (var release in this.scanResult.ReleasesToEncode)
                {
                    List<FileEncodeTask> releaseTasks = new List<FileEncodeTask>();
                    
                    foreach (Track track in release.Tracklist)
                    {
                        string sourceFilename = Path.Combine(this.CollectionManager.Settings.MusicDirectory, track.RelativeFilename);

                        string targetRelativeFilename = FilenameGenerator.PatternToFilename(this.encodingTarget.FileNamingPattern, release, track) + this.encodingTarget.Extension;
                        string targetFilename = Path.Combine(this.encodingTarget.TargetDirectory, targetRelativeFilename);

                        AudioFileTag tag = new AudioFileTag(release, track);

                        FileEncodeTask encodeTask = new FileEncodeTask(
                            encoderFactory,
                            () => AudioHelper.GetAudioSourceForFile(sourceFilename),
                            targetFilename,
                            tag
                            );

                        encodeTask.ProgressChanged += (_sender, _e) =>
                        {
                            if (encodeTask.Status == EncodeTaskStatus.Completed)
                            {
                                FileInfo originalFileInfo = new FileInfo(sourceFilename);
                                File.SetCreationTime(targetFilename, originalFileInfo.CreationTime);
                                File.SetLastWriteTime(targetFilename, originalFileInfo.LastWriteTime);
                                File.SetLastAccessTime(targetFilename, originalFileInfo.LastAccessTime);
                            }
                        };

                        tasks.Add(encodeTask);
                        releaseTasks.Add(encodeTask);
                    }

                    tasks.Add(new ReplayGainTask(replayGainFactory, releaseTasks.ToArray(), false));
                }

                int concurrency = Math.Max(encoderFactory.ThreadCount, replayGainFactory.ThreadCount);
                EncoderController controller = new EncoderController(tasks.ToArray(), concurrency);
                controller.DeleteSuccessfullyEncodedItems = false;
                EncodingWindow window = new EncodingWindow(controller);
                window.ShowDialog(this);
            }

            this.Close();
        }
    }
}
