using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MongoDB.Bson;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using Telerik.Windows.Controls;
using MusicDatabase.Controls.ReleaseViews;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ReleaseTree.xaml
    /// </summary>
    public partial class ReleaseTree : UserControl, IReleaseBrowser
    {
        private const int NumberOfReleasesToExpand = 3;

        private Dictionary<string, TreeReleaseItem> releaseItemByReleaseId = new Dictionary<string, TreeReleaseItem>();
        private SortedDictionary<string, SortedSet<Release>> releasesByAlbumArtist = new SortedDictionary<string, SortedSet<Release>>();
        private ICollectionManager collectionManager;

        public string[] AlbumArtists
        {
            get { return this.releasesByAlbumArtist.Keys.ToArray(); }
        }

        public ObservableCollection<TreeArtistItem> ArtistItems { get; private set; }

        public ICollectionManager CollectionManager
        {
            get { return this.collectionManager; }
            set
            {
                this.collectionManager = value;
            }
        }

        public ReleaseTree()
        {
            InitializeComponent();

            this.ArtistItems = new ObservableCollection<TreeArtistItem>();
            this.treeView.ItemsSource = this.ArtistItems;
            //this.textViewStatisticsHyperlink.Visibility = Visibility.Collapsed;
        }

        private IComparer<Release> GetComparer()
        {
            if (this.collectionManager.Settings.ReleasesViewMode == ReleasesViewMode.AlphabeticalTree)
            {
                return new CallbackComparer<Release>((a, b) =>
                {
                    int result = a.Title.CompareTo(b.Title);
                    if (result == 0) return a.Id == b.Id ? 0 : 1;
                    return result;
                });
            }
            else if (this.collectionManager.Settings.ReleasesViewMode == ReleasesViewMode.ChronologicalTree)
            {
                return new CallbackComparer<Release>((a, b) =>
                {
                    ReleaseDate aDate = a.OriginalReleaseDate.IsValid ? a.OriginalReleaseDate : a.ReleaseDate;
                    ReleaseDate bDate = b.OriginalReleaseDate.IsValid ? b.OriginalReleaseDate : b.ReleaseDate;
                    int result = aDate.Date.CompareTo(bDate.Date);
                    if (result == 0) return a.Id == b.Id ? 0 : a.Title.CompareTo(b.Title);
                    return result;
                });
            }
            else
            {
                if (this.collectionManager.Settings.ReleasesViewMode.ToString().EndsWith("Tree"))
                {
                    throw new NotImplementedException();
                }
                return null;
            }
        }

        public void LoadReleases(Release[] releases)
        {

            this.releaseItemByReleaseId.Clear();
            this.releasesByAlbumArtist.Clear();
            this.ArtistItems.Clear();

            if (releases != null)
            {
                if (this.collectionManager == null)
                {
                    throw new InvalidOperationException("You should set CollectionManager before calling any other methods.");
                }

                bool showImages = this.CollectionManager.Settings.ShowImagesInReleaseTree;

                foreach (Release release in releases)
                {
                    SortedSet<Release> list;
                    if (!this.releasesByAlbumArtist.TryGetValue(release.JoinedAlbumArtists, out list))
                    {
                        this.releasesByAlbumArtist[release.JoinedAlbumArtists] = list = new SortedSet<Release>(this.GetComparer());
                    }
                    list.Add(release);
                }

                foreach (KeyValuePair<string, SortedSet<Release>> albumArtist in this.releasesByAlbumArtist)
                {
                    bool showEmptyImage = albumArtist.Value.Any(r => r.Thumbnail != null);

                    TreeArtistItem artistItem = new TreeArtistItem()
                    {
                        Title = albumArtist.Key,
                        Releases = new List<TreeReleaseItem>()
                    };

                    foreach (Release release in albumArtist.Value)
                    {
                        TreeReleaseItem releaseItem = new TreeReleaseItem(artistItem, release, showImages, showEmptyImage);
                        this.releaseItemByReleaseId[release.Id] = releaseItem;
                        artistItem.Releases.Add(releaseItem);
                    }

                    this.ArtistItems.Add(artistItem);
                }
            }
        }

        private void ClearSelection()
        {
            if (this.treeView.SelectedItem != null)
            {
                if (this.treeView.SelectedItem is TreeArtistItem)
                {
                    ((TreeArtistItem)this.treeView.SelectedItem).IsSelected = false;
                }
                else if (this.treeView.SelectedItem is TreeReleaseItem)
                {
                    ((TreeReleaseItem)this.treeView.SelectedItem).IsSelected = false;
                }
            }
        }

        public bool SetSelectedItem(SelectionInfo info)
        {
            if (info.Type == SelectionInfoType.None)
            {
                this.ClearSelection();
                return false;
            }
            else if (info.Type == SelectionInfoType.Release)
            {
                TreeReleaseItem releaseItem;
                if (this.releaseItemByReleaseId.TryGetValue(info.ReleaseId, out releaseItem))
                {
                    releaseItem.ArtistItem.IsExpanded = true;
                    releaseItem.IsSelected = true;
                    return true;
                }

                return false;
            }
            else if (info.Type == SelectionInfoType.Artist)
            {
                foreach (TreeArtistItem artist in this.ArtistItems)
                {
                    if (artist.Title == info.ArtistName)
                    {
                        artist.IsExpanded = true;
                        artist.IsSelected = true;
                        return true;
                    }
                }
                return false;
            }
            else
            {
                throw new ArgumentException("Unexpected argument type.");
            }
        }

        public SelectionInfo GetSelectedItem()
        {
            if (this.treeView.SelectedItem == null)
            {
                return new SelectionInfo(SelectionInfoType.None);
            }
            else if (this.treeView.SelectedItem is TreeArtistItem)
            {
                TreeArtistItem albumArtist = (TreeArtistItem)this.treeView.SelectedItem;
                return new SelectionInfo(SelectionInfoType.Artist, albumArtist.Title);
            }
            else if (this.treeView.SelectedItem is TreeReleaseItem)
            {
                TreeReleaseItem releaseItem = (TreeReleaseItem)this.treeView.SelectedItem;
                return new SelectionInfo(SelectionInfoType.Release, releaseItem.Release.Id);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public string[] GetReleaseIdsByAlbumArtist(string albumArtist)
        {
            return this.releasesByAlbumArtist[albumArtist].Select(r => r.Id).ToArray();
        }

        private void treeView_SelectedItemChanged_1(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            //this.textViewStatisticsHyperlink.Visibility = this.treeListView.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
            this.OnSelectedItemChanged();
        }

        private void hyperlinkViewStatistics_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            this.ClearSelection();
        }

        public event EventHandler SelectedItemChanged;
        private void OnSelectedItemChanged()
        {
            if (this.SelectedItemChanged != null)
            {
                this.SelectedItemChanged(this, EventArgs.Empty);
            }
        }
    }
}
