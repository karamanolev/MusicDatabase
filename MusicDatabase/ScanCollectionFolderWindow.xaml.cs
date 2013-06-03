using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Local;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ScanCollectionFolderWindow.xaml
    /// </summary>
    public partial class ScanCollectionFolderWindow : MusicDatabaseWindow
    {
        public enum CollectionChangeType
        {
            Addded,
            Changed,
            Deleted
        }

        public class CollectionChangeItem
        {
            public CollectionChangeType Type { get; set; }
            public Release Release { get; set; }
            public LocalAlbum LocalAlbum { get; set; }

            public string ReleaseDisplayName
            {
                get
                {
                    switch (this.Type)
                    {
                        case CollectionChangeType.Addded:
                            return this.LocalAlbum.AlbumArtist + " - " + this.LocalAlbum.Title;
                        case CollectionChangeType.Changed:
                        case CollectionChangeType.Deleted:
                            return this.Release.JoinedAlbumArtists + " - " + this.Release.Title;
                    }
                    return "<error>";
                }
            }
        }

        private Task scannerTask, mergerTask;
        private bool cancelScanning;

        private LocalCollectionMatcherResult matcherResult;

        public ScanCollectionFolderWindow(ICollectionSessionFactory sessionFactory)
            : base(sessionFactory)
        {
            InitializeComponent();

            this.scannerTask = new Task(this.ScannerTask);
            this.scannerTask.Start();
        }

        private void ScannerTask()
        {
            LocalCollectionMatcher matcher = new LocalCollectionMatcher(this.CollectionManager);

            matcher.CollectionScannerProgressChanged +=
                ProgressBarUpdater.CreateHandler(this.Dispatcher, this.progressScan,
                () => this.cancelScanning,
                p => { this.labelScanStatus.Text = p == 1 ? "" : "Reading file tags..."; });
            matcher.ProgressChanged +=
                ProgressBarUpdater.CreateHandler(this.Dispatcher, this.progressMatch,
                () => this.cancelScanning,
                p => { this.labelMatchStatus.Text = p == 1 ? "" : "Matching with database contents..."; });

            this.matcherResult = matcher.Match();

            if (this.cancelScanning)
            {
                return;
            }

            List<CollectionChangeItem> changedItems = new List<CollectionChangeItem>();
            changedItems.AddRange(this.matcherResult.NewReleases.Select(r => new CollectionChangeItem()
            {
                Type = CollectionChangeType.Addded,
                LocalAlbum = r
            }));
            changedItems.AddRange(this.matcherResult.ChangedReleases.Select(r => new CollectionChangeItem()
            {
                Type = CollectionChangeType.Changed,
                Release = r.Item1,
                LocalAlbum = r.Item2
            }));
            changedItems.AddRange(this.matcherResult.DeletedReleases.Select(r => new CollectionChangeItem()
            {
                Type = CollectionChangeType.Deleted,
                Release = r
            }));
            changedItems.Sort((a, b) => a.ReleaseDisplayName.CompareTo(b.ReleaseDisplayName));

            List<CollectionChangeItem> unchangedItems = new List<CollectionChangeItem>();
            unchangedItems.AddRange(this.matcherResult.UnchangedReleases.Select(r => new CollectionChangeItem()
            {
                Type = CollectionChangeType.Deleted,
                Release = r
            }));

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.OnScanningCompleted(changedItems, unchangedItems);
            }));
        }

        private void OnScanningCompleted(List<CollectionChangeItem> changeItems, List<CollectionChangeItem> unchangedItems)
        {
            this.listChanges.ItemsSource = changeItems;
            this.listUnchanged.ItemsSource = unchangedItems;
            if (changeItems.Count > 0)
            {
                this.btnMerge.IsEnabled = changeItems.Count > 0;
            }
            else
            {
                this.EnableCloseButton();
            }
        }

        private void MusicDatabaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!scannerTask.IsCompleted)
            {
                e.Cancel = true;
                if (!this.cancelScanning)
                {
                    this.cancelScanning = true;
                    scannerTask.ContinueWith(t =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.Close();
                        }));
                    });
                }
            }
            if (mergerTask != null && !mergerTask.IsCompleted)
            {
                e.Cancel = true;
                if (!this.cancelScanning)
                {
                    this.cancelScanning = true;
                    mergerTask.ContinueWith(t =>
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.Close();
                        }));
                    });
                }
            }
        }

        private void btnMerge_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            btnMerge.IsEnabled = false;

            this.mergerTask = new Task(MergerTask, this.checkConflictsToFiles.IsChecked == true);
            this.mergerTask.Start();
        }

        private void MergerTask(object saveToFiles)
        {
            LocalCollectionMerger merger = new LocalCollectionMerger(
                this.CollectionManager,
                this.matcherResult,
                (bool)saveToFiles);

            merger.ProgressChanged += ProgressBarUpdater.CreateHandler(this.Dispatcher, this.progressMerge);

            merger.Merge();

            this.Dispatcher.Invoke(new Action(() =>
            {
                CollectionManagerGlobal.OnCollectionChanged();
                EnableCloseButton();
            }));
        }

        private void EnableCloseButton()
        {
            this.btnMerge.Content = "Close";
            this.btnMerge.IsEnabled = true;
            this.btnMerge.Click -= this.btnMerge_Click;
            this.btnMerge.Click += (sender, e) => this.Close();
        }
    }
}
