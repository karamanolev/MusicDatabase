using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MusicDatabase.Advanced;
using MusicDatabase.EncodingTargets;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.ImportExport;
using MusicDatabase.Import;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Audio;
using NHibernate;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MusicDatabaseWindow
    {
        #region Fields

        private ICollectionSessionFactory collectionSessionFactory;
        private CollectionManager collectionManager;

        #endregion


        #region Properties

        public bool HasCollection
        {
            get { return (bool)GetValue(HasCollectionProperty); }
            set { SetValue(HasCollectionProperty, value); }
        }
        public static readonly DependencyProperty HasCollectionProperty =
            DependencyProperty.Register("HasCollection", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(false));

        #endregion


        #region Constructors

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            LibraryLoader.LoadLibraries();
            AudioSourceResolverRegisterer.RegisterResolvers();

            this.DataContext = this;

            InitializeComponent();

            this.UpdateUI();

            if (this.SettingsManager.Settings.CollectionDatabasePath != null)
            {
                this.OpenCollection(this.SettingsManager.Settings.CollectionDatabasePath);
            }
        }

        #endregion


        #region Methods

        private bool IsSupportedFileDrop(string[] files)
        {
            if (files.Length == 1)
            {
                string file = files[0];
                string extension = Path.GetExtension(file).ToLower();
                return extension == ".xml" || extension == ".s3db";
            }

            return false;
        }

        private void FileDrop(string[] files)
        {
            if (files.Length == 1)
            {
                string file = files[0];
                string extension = Path.GetExtension(file).ToLower();

                if (extension == ".xml")
                {
                    this.ViewReleaseFromXml(file);
                }
            }
        }

        public void ViewReleaseFromXml(string file)
        {
            ViewReleaseWindow viewReleaseWindow = new ViewReleaseWindow();
            viewReleaseWindow.LoadReleaseFromXml(file);
            viewReleaseWindow.ShowDialog(this);
        }

        private void CloseCollection()
        {
            if (this.collectionManager != null)
            {
                this.collectionManager.Dispose();
                this.collectionManager = null;
            }

            if (this.collectionSessionFactory != null)
            {
                this.collectionSessionFactory.Dispose();
                this.collectionSessionFactory = null;
            }

            this.mainCollectionView.CollectionSessionFactory = null;
            this.HasCollection = false;

            this.UpdateUI();
        }

        private void OpenCollectionSQLite(string filePath)
        {
            this.OpenCollection(CollectionFactoryFactory.SQLitePrefix + filePath);
        }

        private void OpenCollectionMySQL(string host, string user, string pass, string db)
        {
            this.OpenCollection(CollectionFactoryFactory.MySQLPrefix + CollectionSessionFactory_MySQL.MakeConnectionString(host, user, pass, db));
        }

        private void OpenCollection(string databasePath)
        {
            ICollectionSessionFactory newCollectionSessionFactory;
            CollectionManager newCollectionManager;

            try
            {
                newCollectionSessionFactory = CollectionFactoryFactory.CreateFactory(databasePath);
                newCollectionManager = new CollectionManager(newCollectionSessionFactory);

                if (newCollectionManager.Settings == null && !this.EditCollectionSettings(newCollectionSessionFactory))
                {
                    return;
                }
            }
            catch
            {
                Dialogs.Error("Can not open collection.");
                return;
            }

            this.CloseCollection();

            this.collectionSessionFactory = newCollectionSessionFactory;
            this.collectionManager = newCollectionManager;

            this.SettingsManager.Settings.CollectionDatabasePath = newCollectionSessionFactory.DatabasePath;
            this.SettingsManager.Save();

            this.mainCollectionView.CollectionSessionFactory = newCollectionSessionFactory;
            this.HasCollection = true;

            this.UpdateUI();

            //using (var transaction = this.collectionManager.BeginTransaction())
            //{
            //    foreach (Release release in this.collectionManager.Releases.ToArray())
            //    {
            //        if (release.Tracklist.Any(t => t.RelativeFilename.ToLower().EndsWith(".mp3")))
            //        {
            //            continue;
            //        }

            //        double albumGain = double.NaN;
            //        double albumPeak = double.NaN;

            //        foreach (Track track in release.Tracklist)
            //        {
            //            string filename = track.GetAbsoluteFilename(this.collectionManager);
            //            TagLib.File file = TagLib.File.Create(filename);

            //            if (double.IsNaN(albumGain))
            //            {
            //                albumGain = file.Tag.ReplayGainAlbumGain;
            //            }
            //            else if (file.Tag.ReplayGainAlbumGain != albumGain)
            //            {
            //                throw new Exception();
            //            }

            //            if (double.IsNaN(albumPeak))
            //            {
            //                albumPeak = file.Tag.ReplayGainAlbumPeak;
            //            }
            //            else if (file.Tag.ReplayGainAlbumPeak != albumPeak)
            //            {
            //                throw new Exception();
            //            }

            //            track.ReplayGainTrackGain = file.Tag.ReplayGainTrackGain;
            //            track.ReplayGainTrackPeak = file.Tag.ReplayGainTrackPeak;
            //        }

            //        release.ReplayGainAlbumGain = albumGain;
            //        release.ReplayGainAlbumPeak = albumPeak;
            //    }

            //    transaction.Commit();
            //}
        }

        private void UpdateUI()
        {
            this.UpdateApplicationMenu();
            this.UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            if (this.collectionManager == null)
            {
                this.statusBarDatabase.Content = "No database opened";
                this.statusBarDatabaseInfo.Content = "";
            }
            else
            {
                this.statusBarDatabase.Content = this.collectionSessionFactory.DatabasePath;

                int releases = this.collectionManager.Releases.Count();
                int albumArtists = new HashSet<string>(this.collectionManager.Releases.Select(r => r.JoinedAlbumArtists)).Count;
                this.statusBarDatabaseInfo.Content = releases + " releases, " + albumArtists + " album artists";
            }
        }

        private void UpdateApplicationMenu()
        {
            foreach (MenuItem menuItem in this.menuItemSyncEncodingTargets.Items)
            {
                menuItem.Click -= this.btnSyncEncodingTarget_Click;
            }

            this.menuItemSyncEncodingTargets.Items.Clear();

            if (this.collectionManager == null || this.collectionManager.Settings.EncodingTargets.Count == 0)
            {
                this.menuItemSyncEncodingTargets.Items.Add(new MenuItem()
                {
                    Header = "No Encoding Targets",
                    IsEnabled = false
                });
            }
            else
            {
                foreach (EncodingTarget encodingTarget in this.collectionManager.Settings.EncodingTargets)
                {
                    MenuItem menuItem = new MenuItem()
                    {
                        Header = encodingTarget.TargetDirectory,
                        Tag = encodingTarget
                    };
                    menuItem.Click += new RoutedEventHandler(btnSyncEncodingTarget_Click);
                    this.menuItemSyncEncodingTargets.Items.Add(menuItem);
                }
            }

            this.UpdateApplicationMenuView();
        }

        private void UpdateApplicationMenuView()
        {
            this.menuViewReleaseBrowser.IsEnabled = this.collectionManager != null;
            if (this.collectionManager == null)
            {
                return;
            }

            this.menuBrowserShowImages.IsChecked = this.collectionManager.Settings.ShowImagesInReleaseTree;

            var updateButton = new Action<MenuItem>(item =>
            {
                string val = (string)item.Tag;
                ReleasesViewMode viewMode;
                if (!Enum.TryParse(val, out viewMode))
                {
                    throw new NotImplementedException();
                }
                item.IsChecked = this.collectionManager.Settings.ReleasesViewMode == viewMode;
            });

            this.menuViewReleaseBrowser.Items.Cast<object>().Reverse().TakeWhile(i => !(i is Separator)).Cast<MenuItem>().ForEach(updateButton);
        }

        #endregion


        #region Event Handlers

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Utility.WriteToErrorLog(e.ExceptionObject.ToString());
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Utility.WriteToErrorLog(e.Exception.ToString());
        }

        private void btnSyncEncodingTarget_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)sender;
            EncodingTarget encodingTarget = (EncodingTarget)menuItem.Tag;
            SyncEncodingTargetWindow syncEncodingTargetWindow = new SyncEncodingTargetWindow(this.collectionSessionFactory, encodingTarget.Id);
            syncEncodingTargetWindow.Show(this);
        }

        private void menuNew_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "SQLite 3 Databases (*.s3db)|*.s3db";
            if (saveDialog.ShowDialog() == true)
            {
                this.OpenCollectionSQLite(saveDialog.FileName);
            }
        }

        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog saveDialog = new OpenFileDialog();
            saveDialog.Filter = "SQLite 3 Databases (*.s3db)|*.s3db";
            if (saveDialog.ShowDialog() == true)
            {
                this.OpenCollectionSQLite(saveDialog.FileName);
            }
        }

        private void menuOpenMySQL_Click(object sender, RoutedEventArgs e)
        {
            MySQLConnectionParamsWindow paramsWindow = new MySQLConnectionParamsWindow();
            if (paramsWindow.ShowDialog(this) == true)
            {
                this.OpenCollectionMySQL(paramsWindow.textHost.Text, paramsWindow.textUser.Text, paramsWindow.textPass.Password, paramsWindow.textDatabase.Text);
            }
        }

        private void menuClose_Click(object sender, RoutedEventArgs e)
        {
            this.CloseCollection();
            this.SettingsManager.Settings.CollectionDatabasePath = null;
            this.SettingsManager.Save();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.collectionManager != null)
            {
                this.collectionManager.Dispose();
            }
            if (this.collectionSessionFactory != null)
            {
                this.collectionSessionFactory.Dispose();
            }
            this.mainCollectionView.Dispose();
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void menuScanCollectionFolder_Click(object sender, RoutedEventArgs e)
        {
            this.ScanCollectionFolder();
        }

        private void ScanCollectionFolder()
        {
            new ScanCollectionFolderWindow(this.collectionSessionFactory).ShowDialog(this);
        }

        private void menuCollectionSettings_Click(object sender, RoutedEventArgs e)
        {
            this.EditCollectionSettings(this.collectionSessionFactory);
        }

        private bool EditCollectionSettings(ICollectionSessionFactory collectionSessionFactory)
        {
            EditCollectionSettingsWindow editCollectionSettingsWindow = new EditCollectionSettingsWindow(collectionSessionFactory);
            return editCollectionSettingsWindow.ShowDialog(this) == true;
        }

        protected override void OnMusicCollectionChanged()
        {
            base.OnMusicCollectionChanged();

            this.collectionManager.ClearCache();
            this.mainCollectionView.OnCollectionChanged();
            this.UpdateUI();
        }

        protected override void OnSettingsChanged()
        {
            base.OnSettingsChanged();

            if (this.collectionManager != null)
            {
                this.collectionManager.ClearCache();
                this.mainCollectionView.OnCollectionChanged();
                this.UpdateUI();
            }
        }

        private void menuImportTracks_Click(object sender, RoutedEventArgs e)
        {
            this.ImportTracks();
        }

        private void ImportTracks()
        {
            ImportTracksWindow window = new ImportTracksWindow(this.collectionSessionFactory);
            if (window.ShowDialog(this) == true)
            {
                this.mainCollectionView.SetSelectedItem(window.InsertedReleaseId);
            }
        }

        private void menuClearWindowSettings_Click(object sender, RoutedEventArgs e)
        {
            this.SettingsManager.Settings.WindowsSettings.Clear();
            this.SettingsManager.Save();
        }

        private void menuAudioChecksum_Click(object sender, RoutedEventArgs e)
        {
            AudioChecksumWindow audioChecksumWindow = new AudioChecksumWindow();
            audioChecksumWindow.ShowDialog(this);
        }

        private void menuServerDiscovery_Click(object sender, RoutedEventArgs e)
        {
            ServerDiscoveryWindow serverDiscoveryWindow = new ServerDiscoveryWindow();
            serverDiscoveryWindow.ShowDialog(this);
        }

        private void menuExportArchivedXml_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Zip Files (*.zip)|*.zip|" + Utility.AllFilesFilter;
            saveFileDialog.FileName = "Export_" + DateTime.Now.ToString("yyyy_MM_dd") + ".zip";
            if (saveFileDialog.ShowDialog() == true)
            {
                WaitWindow waitWindow = new WaitWindow("Exporting collection...");
                waitWindow.ShowDialog(this, () =>
                {
                    using (CollectionManager exportManager = new Engine.CollectionManager(this.collectionSessionFactory))
                    {
                        using (CollectionExporterBase exporter = new ArchivedCollectionExporter(saveFileDialog.FileName, exportManager))
                        {
                            exporter.Export();
                        }
                    }
                });
            }
        }

        private void menuExportXml_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = folderDialog.SelectedPath;

                if (Directory.GetFiles(path).Length != 0 || Directory.GetDirectories(path).Length != 0)
                {
                    MessageBoxResult emptyDirectoryResult = Dialogs.YesNoCancelQuestion("Target directory is not empty. Delete directory contents before exporting?");
                    if (emptyDirectoryResult == MessageBoxResult.Yes)
                    {
                        if (Directory.GetFiles(path, "*", SearchOption.AllDirectories).Any(f => Path.GetExtension(f).ToLower() != ".xml"))
                        {
                            Dialogs.Error("The directory contains files that aren't XML. I refuse to delete them!");
                            return;
                        }

                        if (!Utility.TryEmptyDirectory(path))
                        {
                            Dialogs.Error("Error deleting directory contents!");
                            return;
                        }
                    }
                    else if (emptyDirectoryResult == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                WaitWindow waitWindow = new WaitWindow("Exporting collection...");
                waitWindow.ShowDialog(this, () =>
                {
                    using (CollectionManager exportManager = new Engine.CollectionManager(this.collectionSessionFactory))
                    {
                        using (DirectoryCollectionExporter exporter = new DirectoryCollectionExporter(folderDialog.SelectedPath, exportManager))
                        {
                            exporter.Export();
                        }
                    }
                });
            }
        }

        private void menuImportXml_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                WaitWindow waitWindow = new WaitWindow("Importing collection...");
                waitWindow.ShowDialog(this, () =>
                {
                    try
                    {
                        using (CollectionManager exportManager = new Engine.CollectionManager(this.collectionSessionFactory))
                        {
                            using (DirectoryCollectionImporter importer = new DirectoryCollectionImporter(folderDialog.SelectedPath, exportManager, UIHelper.UpdateReleaseThumbnail))
                            {
                                importer.Import();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Utility.WriteToErrorLog("Error importing: " + ex.ToString());
                        MessageBox.Show("Error importing backup: " + ex.Message);
                    }

                    this.Dispatcher.BeginInvokeAction(() =>
                    {
                        CollectionManager.OnCollectionChanged();
                    });
                });
            }
        }

        private void menuImportArchivedXml_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Zip Files (*.zip)|*.zip|" + Utility.AllFilesFilter;
            if (openFileDialog.ShowDialog() == true)
            {
                WaitWindow waitWindow = new WaitWindow("Importing collection...");
                waitWindow.ShowDialog(this, () =>
                {
                    using (CollectionManager importManager = new Engine.CollectionManager(this.collectionSessionFactory))
                    {
                        try
                        {
                            using (CollectionImporterBase importer = new ArchivedCollectionImporter(openFileDialog.FileName, importManager, UIHelper.UpdateReleaseThumbnail))
                            {
                                importer.Import();
                            }
                        }
                        catch (Exception ex)
                        {
                            Utility.WriteToErrorLog("Error importing: " + ex.ToString());
                            MessageBox.Show("Error importing backup: " + ex.Message);
                        }
                    }

                    this.Dispatcher.BeginInvokeAction(() =>
                    {
                        CollectionManager.OnCollectionChanged();
                    });
                });
            }
        }

        private void btnReplaceReleaseFiles_Click(object sender, RoutedEventArgs e)
        {
            object selectedItem = this.mainCollectionView.GetSelectedItem();
            if (selectedItem is int)
            {
                int releaseId = (int)selectedItem;
                ReplaceReleaseFilesWindow replaceReleaseFilesWindow = new ReplaceReleaseFilesWindow(this.collectionSessionFactory, releaseId);
                replaceReleaseFilesWindow.ShowDialog(this);
            }
            else
            {
                Dialogs.Inform("Please select a release.");
            }
        }

        private void menuStatistics_Click(object sender, RoutedEventArgs e)
        {
            new StatisticsWindow(this.collectionSessionFactory).ShowDialog(this);
        }

        private void btnFixMissingFields_Click(object sender, RoutedEventArgs e)
        {
            string message;
            this.collectionManager.Operations.FixMissingFields(out message);
            Dialogs.Inform(message);
        }

        private void MusicDatabaseWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (this.IsSupportedFileDrop(files))
                {
                    e.Effects = DragDropEffects.All;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
        }

        private void MusicDatabaseWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (this.IsSupportedFileDrop(files))
                {
                    this.FileDrop(files);
                }
            }
        }

        private void btnCrash_Click(object sender, RoutedEventArgs e)
        {
            throw new FormatException("Intentional crash!");
        }

        private void MusicDatabaseWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.mainCollectionView.FocusSearchField();
        }

        private void btnFillDiscogsMasterIds_Click(object sender, RoutedEventArgs e)
        {
            new FillReleaseDiscogsMasterIdsWindow(this.collectionSessionFactory).ShowDialog(this);
        }

        private void menuBrowserShowImages_Click(object sender, RoutedEventArgs e)
        {
            this.collectionManager.Settings.ShowImagesInReleaseTree = menuBrowserShowImages.IsChecked;
            this.collectionManager.SaveSettings();
            CollectionManager.OnCollectionChanged();
        }

        private void menuBrowserViewMode_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            ReleasesViewMode viewMode;
            Assert.IsTrue(Enum.TryParse((string)menuItem.Tag, out viewMode));
            this.collectionManager.Settings.ReleasesViewMode = viewMode;
            this.collectionManager.SaveSettings();
            CollectionManager.OnCollectionChanged();
        }

        private void btnUpdateReleasesDynamicProperties_Click(object sender, RoutedEventArgs e)
        {
            Progress<double> progress = new Progress<double>();
            new WaitWindow("Update releases properties...").ShowDialog(this, new Task(() =>
            {
                this.collectionManager.Operations.UpdateReleasesDynamicProperties(progress);
                this.Dispatcher.BeginInvokeAction(() => CollectionManager.OnCollectionChanged());
            }), progress);
        }

        private void btnUpdateReleasesThumbnails_Click(object sender, RoutedEventArgs e)
        {
            Progress<double> progress = new Progress<double>();
            new WaitWindow("Update releases properties...").ShowDialog(this, new Task(() =>
            {
                this.collectionManager.Operations.UpdateReleasesThumbnails(progress, r => UIHelper.UpdateReleaseThumbnail(r, this.collectionManager.ImageHandler));
                this.Dispatcher.BeginInvokeAction(() => CollectionManager.OnCollectionChanged());
            }), progress);
        }

        private void btnTestImportExportReleases_Click(object sender, RoutedEventArgs e)
        {
            var window = new TestImportExportReleases(this.collectionSessionFactory);
            window.Show(this);
        }

        private void btnAudioFormatAnalysis_Click(object sender, RoutedEventArgs e)
        {
            var window = new AudioFormatAnalysisWindow(this.collectionSessionFactory);
            window.Show(this);
        }

        private void btnRecalculateReplayGain_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<Release, ReplayGainTask> releaseToTask = new Dictionary<Release, ReplayGainTask>();
            Dictionary<Track, FileEncodeTask> trackToTask = new Dictionary<Track, FileEncodeTask>();
            List<IParallelTask> tasks = new List<IParallelTask>();

            Progress<double> progress = new Progress<double>();
            new WaitWindow("Generating tasks...").ShowDialog(this, new Task(() =>
            {
                this.GenerateReplayGainTasks(releaseToTask, trackToTask, tasks, progress);
            }), progress);

            EncoderController encoderController = new EncoderController(tasks.ToArray(), this.SettingsManager.Settings.ActualLocalConcurrencyLevel);

            EncodingWindow encodingWindow = new EncodingWindow(encoderController);
            if (encodingWindow.ShowDialog(this) == true)
            {
                using (var transaction = this.collectionManager.BeginTransaction())
                {
                    foreach (KeyValuePair<Release, ReplayGainTask> item in releaseToTask)
                    {
                        if (item.Value.AlbumGain != null)
                        {
                            item.Key.ReplayGainAlbumGain = item.Value.AlbumGain.GetGain();
                            item.Key.ReplayGainAlbumPeak = item.Value.AlbumGain.GetPeak();
                        }
                    }

                    foreach (KeyValuePair<Track, FileEncodeTask> item in trackToTask)
                    {
                        if (item.Value.TrackGain != null)
                        {
                            item.Key.ReplayGainTrackGain = item.Value.TrackGain.GetGain();
                            item.Key.ReplayGainTrackPeak = item.Value.TrackGain.GetPeak();
                        }
                    }
                }
            }
        }

        private void GenerateReplayGainTasks(Dictionary<Release, ReplayGainTask> releaseToTask, Dictionary<Track, FileEncodeTask> trackToTask, List<IParallelTask> tasks, IProgress<double> progress)
        {
            DspEncoderFactory replayGainFactory = new DspEncoderFactory(this.SettingsManager.Settings.LocalConcurrencyLevel, true, false);
            using (CollectionManager innerManager = new CollectionManager(this.collectionSessionFactory))
            {
                Release[] releases = innerManager.Releases.ToArray();
                double progressCoef = 1.0 / releases.Length;
                int processed = 0;
                foreach (Release release in releases)
                {
                    List<FileEncodeTask> releaseTasks = new List<FileEncodeTask>();

                    foreach (Track track in release.Tracklist)
                    {
                        string filename = Path.Combine(innerManager.Settings.MusicDirectory, track.RelativeFilename);
                        var task = new FileEncodeTask(replayGainFactory, () => AudioHelper.GetAudioSourceForFile(filename), filename, null);

                        tasks.Add(task);
                        releaseTasks.Add(task);
                        trackToTask[track] = task;
                    }

                    ReplayGainTask rgTask = new ReplayGainTask(replayGainFactory, releaseTasks.ToArray(), true);
                    tasks.Add(rgTask);
                    releaseToTask[release] = rgTask;

                    ++processed;
                    progress.Report(processed * progressCoef);

                }
            }
        }

        private void btnRecalculateDynamicRange_Click(object sender, RoutedEventArgs e)
        {
            using (CollectionManager innerManager = new CollectionManager(this.collectionSessionFactory))
            {
                Dictionary<Track, FileEncodeTask> trackToTask = new Dictionary<Track, FileEncodeTask>();
                List<FileEncodeTask> tasks = new List<FileEncodeTask>();

                Progress<double> progress = new Progress<double>();
                new WaitWindow("Generating tasks...").ShowDialog(this, new Task(() =>
                {
                    this.GenerateDynamicRangeTasks(innerManager, tasks, trackToTask, progress);
                }), progress);

                EncoderController encoderController = new EncoderController(tasks.ToArray(), this.SettingsManager.Settings.ActualLocalConcurrencyLevel);

                EncodingWindow encodingWindow = new EncodingWindow(encoderController);
                if (encodingWindow.ShowDialog(this) == true)
                {
                    using (ITransaction transaction = innerManager.BeginTransaction())
                    {
                        foreach (KeyValuePair<Track, FileEncodeTask> items in trackToTask)
                        {
                            if (items.Value.DrMeter != null)
                            {
                                items.Key.DynamicRange = items.Value.DrMeter.GetDynamicRange();
                            }
                        }

                        foreach (Release release in innerManager.Releases)
                        {
                            release.UpdateDynamicProperties();
                        }

                        transaction.Commit();
                    }

                    CollectionManager.OnCollectionChanged();
                }
            }
        }

        private void GenerateDynamicRangeTasks(CollectionManager manager, List<FileEncodeTask> tasks, Dictionary<Track, FileEncodeTask> trackToTask, IProgress<double> progress)
        {
            DspEncoderFactory replayGainFactory = new DspEncoderFactory(this.SettingsManager.Settings.LocalConcurrencyLevel, false, true);

            Release[] releases = manager.Releases.ToArray();
            double progressCoef = 1.0 / releases.Length;
            int processed = 0;
            foreach (Release release in releases)
            {
                foreach (Track track in release.Tracklist)
                {
                    string filename = Path.Combine(manager.Settings.MusicDirectory, track.RelativeFilename);
                    var task = new FileEncodeTask(replayGainFactory, () => AudioHelper.GetAudioSourceForFile(filename), filename, null);

                    trackToTask[track] = task;
                    tasks.Add(task);
                }

                ++processed;
                progress.Report(processed * progressCoef);
            }
        }

        #endregion
    }
}