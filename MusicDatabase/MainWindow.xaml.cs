using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MusicDatabase.Advanced;
using MusicDatabase.EncodingTargets;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Helpers;
using MusicDatabase.Import;
using Microsoft.Win32;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MusicDatabaseWindow
    {
        #region Fields

        private ICollectionSessionFactory collectionSessionFactory;
        private ICollectionManager collectionManager;

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

        private void OpenCollection(string databasePath)
        {
            ICollectionSessionFactory newCollectionSessionFactory;
            ICollectionManager newCollectionManager;

            try
            {
                newCollectionSessionFactory = CollectionFactoryFactory.CreateFactory(databasePath);
                newCollectionManager = newCollectionSessionFactory.CreateCollectionManager();

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

                int releases = this.collectionManager.ReleaseCount;
                int albumArtists = this.collectionManager.Releases.Select(r => r.JoinedAlbumArtists).Distinct().ToArray().Length;
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
            SyncEncodingTargetWindow syncEncodingTargetWindow = new SyncEncodingTargetWindow(this.collectionSessionFactory, encodingTarget);
            syncEncodingTargetWindow.Show(this);
        }

        private void menuNew_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "SQLite 3 Databases (*.s3db)|*.s3db";
            if (saveDialog.ShowDialog() == true)
            {
                this.OpenCollection(CollectionFactoryFactory.SQLitePrefix + saveDialog.FileName);
            }
        }

        private void menuOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog saveDialog = new OpenFileDialog();
            saveDialog.Filter = "SQLite 3 Databases (*.s3db)|*.s3db";
            if (saveDialog.ShowDialog() == true)
            {
                this.OpenCollection(CollectionFactoryFactory.SQLitePrefix + saveDialog.FileName);
            }
        }

        private void menuOpenMongo_Click(object sender, RoutedEventArgs e)
        {
            DatabaseConnectionParamsWindow paramsWindow = new DatabaseConnectionParamsWindow();
            if (paramsWindow.ShowDialog(this) == true)
            {
                this.OpenCollection(paramsWindow.ConnectionString);
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
                this.mainCollectionView.SetSelectedItem(SelectionInfo.Release(window.InsertedReleaseId));
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
            using (ArchivedExportHelper helper = new ArchivedExportHelper(this, this.collectionSessionFactory))
            {
                helper.Run();
            }
        }

        private void menuExportXml_Click(object sender, RoutedEventArgs e)
        {
            using (DirectoryExportHelper helper = new DirectoryExportHelper(this, this.collectionSessionFactory))
            {
                helper.Run();
            }
        }

        private void menuImportXml_Click(object sender, RoutedEventArgs e)
        {
            using (DirectoryImportHelper helper = new DirectoryImportHelper(this, this.collectionSessionFactory))
            {
                helper.Run();
            }
        }

        private void menuImportArchivedXml_Click(object sender, RoutedEventArgs e)
        {
            using (ArchivedImportHelper helper = new ArchivedImportHelper(this, this.collectionSessionFactory))
            {
                helper.Run();
            }
        }

        private void btnReplaceReleaseFiles_Click(object sender, RoutedEventArgs e)
        {
            object selectedItem = this.mainCollectionView.GetSelectedItem();
            if (selectedItem is string)
            {
                string releaseId = (string)selectedItem;
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
            CollectionManagerGlobal.OnCollectionChanged();
        }

        private void menuBrowserViewMode_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuItem = (MenuItem)e.Source;
            ReleasesViewMode viewMode;
            Assert.IsTrue(Enum.TryParse((string)menuItem.Tag, out viewMode));
            this.collectionManager.Settings.ReleasesViewMode = viewMode;
            this.collectionManager.SaveSettings();
            CollectionManagerGlobal.OnCollectionChanged();
        }

        private void btnUpdateReleasesDynamicProperties_Click(object sender, RoutedEventArgs e)
        {
            Progress<double> progress = new Progress<double>();
            new WaitWindow("Update releases properties...").ShowDialog(this, new Task(() =>
            {
                this.collectionManager.Operations.UpdateReleasesDynamicProperties(progress);
                this.Dispatcher.BeginInvokeAction(() => CollectionManagerGlobal.OnCollectionChanged());
            }), progress);
        }

        private void btnUpdateReleasesThumbnails_Click(object sender, RoutedEventArgs e)
        {
            Progress<double> progress = new Progress<double>();
            new WaitWindow("Update releases properties...").ShowDialog(this, new Task(() =>
            {
                this.collectionManager.Operations.UpdateReleasesThumbnails(progress);
                this.Dispatcher.BeginInvokeAction(() => CollectionManagerGlobal.OnCollectionChanged());
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

        private void btnRecalculateReplayGainAll_Click(object sender, RoutedEventArgs e)
        {
            using (ReplayGainUpdateHelper updater = new ReplayGainUpdateHelper(this, this.SettingsManager, this.collectionSessionFactory))
            {
                updater.RunAllReleases();
            }
        }

        private void btnRecalculateReplayGainThis_Click(object sender, RoutedEventArgs e)
        {
            object selectedItem = this.mainCollectionView.GetSelectedItem();
            if (selectedItem is string)
            {
                string releaseId = (string)selectedItem;
                Release release = this.collectionManager.GetReleaseById(releaseId);

                using (ReplayGainUpdateHelper updater = new ReplayGainUpdateHelper(this, this.SettingsManager, this.collectionSessionFactory))
                {
                    updater.RunOneRelease(release);
                }
            }
            else
            {
                Dialogs.Inform("Please select a release.");
            }
        }

        private void btnRecalculateDynamicRangeAll_Click(object sender, RoutedEventArgs e)
        {
            using (DynamicRangeUpdater updater = new DynamicRangeUpdater(this, this.SettingsManager, this.collectionSessionFactory))
            {
                updater.RunAllReleases();
            }
        }

        private void btnRecalculateDynamicRangeThis_Click(object sender, RoutedEventArgs e)
        {
            object selectedItem = this.mainCollectionView.GetSelectedItem();
            if (selectedItem is string)
            {
                string releaseId = (string)selectedItem;
                Release release = this.collectionManager.GetReleaseById(releaseId);

                using (DynamicRangeUpdater updater = new DynamicRangeUpdater(this, this.SettingsManager, this.collectionSessionFactory))
                {
                    updater.RunOneRelease(release);
                }
            }
            else
            {
                Dialogs.Inform("Please select a release.");
            }
        }

        private void btnFillMissingCoverArt_Click(object sender, RoutedEventArgs e)
        {
            using (MissingCoverArtFillHelper helper = new MissingCoverArtFillHelper(this, this.collectionSessionFactory))
            {
                helper.Run();
            }
        }

        #endregion
    }
}