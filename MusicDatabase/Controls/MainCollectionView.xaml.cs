using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    public partial class MainCollectionView : UserControl, IDisposable
    {
        public class CustomListViewItem
        {
            public string Disc { get; set; }
            public string Pos { get; set; }
            public string JoinedArtists { get; set; }
            public string Title { get; set; }
            public FontWeight FontWeight { get; set; }
            public FontStyle FontStyle { get; set; }
        }

        private Release[] releases;
        private MainCollectionViewOperations operations;
        private DelayedExecution delayedExecution;
        private ICollectionSessionFactory collectionSessionFactory;
        private ICollectionManager collectionManager;
        private Task backgroundTask;
        private ReleaseFilter releaseFilter;
        private IReleaseBrowser currentReleaseBrowser;

        private bool cancelSearch = false;
        private bool isLoading = false;
        private SelectionInfo releaseToSelect;

        public ICollectionManager CollectionManager
        {
            get { return this.collectionManager; }
        }

        public ICollectionSessionFactory CollectionSessionFactory
        {
            get { return this.collectionSessionFactory; }
            set
            {
                this.collectionSessionFactory = value;

                if (this.collectionManager != null)
                {
                    this.collectionManager.Dispose();
                    this.collectionManager = null;
                }

                if (this.collectionSessionFactory != null)
                {
                    this.collectionManager = this.collectionSessionFactory.CreateCollectionManager();
                    this.releaseDetailsView.CollectionManager = this.collectionManager;
                    this.releaseTree.CollectionManager = this.collectionManager;
                    this.releaseList.CollectionManager = this.collectionManager;

                    this.collectionStatistics.UpdateUI();
                }

                this.ReloadReleases();
                this.UpdateSelectedRelease();
            }
        }

        public MainCollectionView()
        {
            this.operations = new MainCollectionViewOperations(this);
            this.releaseFilter = new ReleaseFilter();
            this.releaseToSelect = new SelectionInfo(SelectionInfoType.None);

            InitializeComponent();

            this.collectionStatistics.Init(this);

            this.ResetFilter();

            this.delayedExecution = new DelayedExecution(() =>
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    this.ReloadReleases();
                }));
            }, TimeSpan.FromMilliseconds(300));

            this.UpdateSelectedRelease();
            this.groupFilter.Visibility = Visibility.Collapsed;
        }

        public void FocusSearchField()
        {
            this.textFilter.Focus();
        }

        public void Dispose()
        {
            if (this.collectionManager != null)
            {
                this.collectionManager.Dispose();
                this.collectionManager = null;
            }
        }

        private void ResetFilter()
        {
            this.checkFilterHasImages.IsChecked = null;
            this.checkFilterHasFiles.IsChecked = null;
            this.checkFilterIsFlagged.IsChecked = null;
            this.checkFilterHasWikiPage.IsChecked = null;
            this.checkFilterDoExtendedSearch.IsChecked = false;
        }

        private void UpdateFilterDetails()
        {
            this.releaseFilter.SearchString = this.textFilter.Text;
            this.releaseFilter.AdditionalFiltering = this.btnToggleFilter.IsChecked == true;
            this.releaseFilter.HasImages = this.checkFilterHasImages.IsChecked;
            this.releaseFilter.HasFiles = this.checkFilterHasFiles.IsChecked;
            this.releaseFilter.IsFlagged = this.checkFilterIsFlagged.IsChecked;
            this.releaseFilter.HasWikiPage = this.checkFilterHasWikiPage.IsChecked;
            this.releaseFilter.DoExtendedSearch = this.checkFilterDoExtendedSearch.IsChecked == true;
        }

        public void SpecialSearch(string name, Func<Release, bool> filter)
        {
            this.releaseFilter.FilterFunction = filter;
            this.textFilter.Text = "{ " + name + " }";
        }

        public void OnCollectionChanged()
        {
            this.releases = null;
            this.ClearCaches();
            this.ReloadReleases();
            this.collectionStatistics.UpdateUI();
        }

        private void ReloadReleases()
        {
            Task localBackgroundTask = this.backgroundTask;
            if (localBackgroundTask != null)
            {
                this.cancelSearch = true;
                localBackgroundTask.Wait();
                this.cancelSearch = false;
            }

            this.isLoading = true;
            if (this.currentReleaseBrowser != null)
            {
                this.releaseToSelect = this.currentReleaseBrowser.GetSelectedItem();
            }

            this.releaseTree.LoadReleases(null);
            this.releaseList.LoadReleases(null);

            this.releaseSelectorBusyIndicator.IsBusy = this.collectionManager != null;

            this.releaseTree.IsEnabled = this.collectionManager != null;
            this.releaseList.IsEnabled = this.collectionManager != null;

            this.textFilter.IsEnabled = this.collectionManager != null;
            this.tracklistView.IsEnabled = this.collectionManager != null;

            this.UpdateFilterDetails();

            if (this.collectionManager != null)
            {
                this.UpdateCurrentBrowser();

                Release[] filteredReleases = null;

                this.backgroundTask = new Task(() =>
                {
                    if (this.releases == null)
                    {
                        this.releases = this.collectionManager.Releases.ToArray();
                    }
                    filteredReleases = this.releases.Where(r =>
                    {
                        if (this.cancelSearch) return false;
                        return this.releaseFilter.Match(r);
                    }).ToArray();

                    if (!this.cancelSearch)
                    {
                        this.Dispatcher.BeginInvokeAction(() =>
                        {
                            this.collectionStatistics.Releases = this.releases;

                            if (filteredReleases != null)
                            {
                                DisplayReleases(filteredReleases);
                            }

                            this.releaseSelectorBusyIndicator.IsBusy = false;
                        });
                    }
                    this.backgroundTask = null;
                });

                this.backgroundTask.Start();
            }
        }

        private void UpdateCurrentBrowser()
        {
            if (this.CollectionManager.Settings.ReleasesViewMode.ToString().EndsWith("Tree"))
            {
                this.releaseTree.Visibility = Visibility.Visible;
                this.releaseList.Visibility = Visibility.Collapsed;

                this.currentReleaseBrowser = this.releaseTree;
                this.releaseList.LoadReleases(null);
            }
            else if (this.CollectionManager.Settings.ReleasesViewMode.ToString().EndsWith("List"))
            {
                this.releaseTree.Visibility = Visibility.Collapsed;
                this.releaseList.Visibility = Visibility.Visible;

                this.currentReleaseBrowser = this.releaseList;
                this.releaseTree.LoadReleases(null);
            }
        }

        public Release GetLocalRelease(string id)
        {
            return this.collectionManager.GetReleaseById((id));
        }

        private void DisplayReleases(Release[] releases)
        {
            this.UpdateCurrentBrowser();

            this.currentReleaseBrowser.LoadReleases(releases);

            this.isLoading = false;

            if (!this.currentReleaseBrowser.SetSelectedItem(this.releaseToSelect))
            {
                string[] albumArtists = this.currentReleaseBrowser.AlbumArtists;

                if (albumArtists == null)
                {
                    if (releases.Length != 0)
                    {
                        this.currentReleaseBrowser.SetSelectedItem(SelectionInfo.Release(releases[0].Id));
                    }
                }
                else if (albumArtists.Length == 1)
                {
                    this.currentReleaseBrowser.SetSelectedItem(SelectionInfo.Artist(albumArtists[0]));
                }
            }
        }

        public object GetSelectedItem()
        {
            return this.currentReleaseBrowser.GetSelectedItem();
        }

        public void SetSelectedItem(SelectionInfo info)
        {
            if (this.isLoading)
            {
                this.releaseToSelect = info;
            }
            else
            {
                this.currentReleaseBrowser.SetSelectedItem(info);
            }
        }

        private bool UpdateSelectedRelease()
        {
            if (this.collectionManager == null)
            {
                this.tracklistView.Visibility = Visibility.Collapsed;
                this.collectionStatistics.Visibility = Visibility.Collapsed;
                this.releaseDetailsView.Visibility = Visibility.Collapsed;
                this.releaseDetailsView.Release = null;
                this.releaseCoverFlow.Visibility = Visibility.Collapsed;
                this.releaseCoverFlow.Releases = null;

                return true;
            }

            SelectionInfo selectedItem = this.currentReleaseBrowser.GetSelectedItem();
            if (selectedItem.Type == SelectionInfoType.None)
            {
                this.tracklistView.Releases = null;

                this.tracklistView.Visibility = Visibility.Collapsed;
                this.collectionStatistics.Visibility = Visibility.Visible;
                this.releaseDetailsView.Visibility = Visibility.Collapsed;
                this.releaseDetailsView.Release = null;
                this.releaseCoverFlow.Visibility = Visibility.Collapsed;
                this.releaseCoverFlow.Releases = null;
            }
            else if (selectedItem.Type == SelectionInfoType.Release)
            {
                Release release = this.GetLocalRelease(selectedItem.ReleaseId);

                this.tracklistView.Releases = new Release[] { release };

                this.tracklistView.Visibility = Visibility.Visible;
                this.collectionStatistics.Visibility = Visibility.Collapsed;
                this.releaseDetailsView.Visibility = Visibility.Visible;
                this.releaseDetailsView.Release = release;
                this.releaseCoverFlow.Visibility = Visibility.Collapsed;
                this.releaseCoverFlow.Releases = null;
            }
            else if (selectedItem.Type == SelectionInfoType.Artist)
            {
                var releaseIds = this.currentReleaseBrowser.GetReleaseIdsByAlbumArtist(selectedItem.ArtistName);
                Release[] releases = releaseIds.Select(r => this.GetLocalRelease(r)).Where(r => r != null).ToArray();
                this.tracklistView.Releases = releases;

                this.tracklistView.Visibility = Visibility.Visible;
                this.collectionStatistics.Visibility = Visibility.Collapsed;
                if (releases.Length == 1)
                {
                    this.releaseDetailsView.Visibility = Visibility.Visible;
                    this.releaseDetailsView.Release = releases[0];
                    this.releaseCoverFlow.Visibility = Visibility.Collapsed;
                    this.releaseCoverFlow.Releases = null;
                }
                else
                {
                    this.releaseDetailsView.Visibility = Visibility.Collapsed;
                    this.releaseDetailsView.Release = null;
                    this.releaseCoverFlow.Visibility = Visibility.Visible;
                    this.releaseCoverFlow.Releases = releases;
                    this.releaseCoverFlow.SelectedIndex = (releases.Length - 1) / 2;
                }
            }
            return false;
        }

        private void releaseCoverFlow_ItemSelected(object sender, EventArgs e)
        {
            this.SetSelectedItem(SelectionInfo.Release(this.releaseCoverFlow.Releases[this.releaseCoverFlow.SelectedIndex].Id));
        }

        public void ClearCaches()
        {
            if (this.collectionManager != null)
            {
                this.collectionManager.ClearCache();
            }
        }

        public void ClearSearch()
        {
            this.textFilter.Text = "";
        }

        #region UI Handlers

        private void textFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.btnClearSearch.Visibility = this.textFilter.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            this.delayedExecution.Enable();
        }

        private void releaseBrowser_SelectedItemChanged(object sender, EventArgs e)
        {
            UpdateSelectedRelease();
        }

        private void releaseDetailsView_EditReleaseClicked(object sender, System.EventArgs e)
        {
            string id = this.releaseDetailsView.Release.Id;
            this.operations.EditRelease(id);
            this.SetSelectedItem(SelectionInfo.Release(id));
        }

        private void releaseDetailsView_DeleteReleaseClicked(object sender, System.EventArgs e)
        {
            this.operations.DeleteRelease(this.releaseDetailsView.Release);
        }

        private void releaseDetailsView_ToggleReleaseFlagClicked(object sender, EventArgs e)
        {
            this.operations.ToggleReleaseFlag(this.releaseDetailsView.Release);
        }

        private void releaseDetailsView_PlayClicked(object sender, EventArgs e)
        {
            this.operations.PlayRelease(this.releaseDetailsView.Release);
        }

        private void btnToggleFilter_CheckedChanged(object sender, EventArgs e)
        {
            this.groupFilter.Visibility = this.btnToggleFilter.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            this.ReloadReleases();
        }

        private void filterControl_StateChanged(object sender, EventArgs e)
        {
            this.ReloadReleases();
        }

        private void btnResetFilter_Clicked(object sender, RoutedEventArgs e)
        {
            this.ResetFilter();
        }

        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            ClearSearch();
        }

        private void releaseDetailsView_ChecksumClicked(object sender, EventArgs e)
        {
            this.operations.ComputeReleaseChecksum(this.releaseDetailsView.Release);
        }

        private void releaseDetailsView_ExploreClicked(object sender, EventArgs e)
        {
            this.operations.ExploreRelease(this.releaseDetailsView.Release);
        }

        private void releaseDetailsView_RemoveReleaseClicked(object sender, EventArgs e)
        {
            this.operations.RemoveRelease(this.releaseDetailsView.Release);
        }

        #endregion
    }
}
