using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using Telerik.Windows.Controls;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ReleaseTree.xaml
    /// </summary>
    public partial class ReleaseTree : UserControl, IReleaseBrowser
    {
        public class AlbumArtistItem
        {
            public string Title { get; set; }
            public string Year
            {
                get { return ""; }
            }
            public BitmapSource ImageSource
            {
                get { return null; }
            }
            public System.Windows.Controls.Image Image
            {
                get { return null; }
            }
            public List<ReleaseItem> Releases { get; set; }
        }

        public class ReleaseItem
        {
            private bool showImage;
            private bool showEmptyImage;
            private System.Windows.Controls.Image image;

            public AlbumArtistItem AlbumArtistItem { get; private set; }
            public Release Release { get; private set; }

            public string Title
            {
                get { return Release.Title; }
            }

            public string Year
            {
                get
                {
                    if (this.Release.OriginalReleaseDate != null && this.Release.OriginalReleaseDate.IsValid)
                    {
                        return this.Release.OriginalReleaseDate.Date.Year.ToString();
                    }
                    return this.Release.ReleaseDate.IsValid ? this.Release.ReleaseDate.Date.Year.ToString() : "";
                }
            }

            public System.Windows.Controls.Image Image
            {
                get
                {
                    if (!this.showImage)
                    {
                        return null;
                    }
                    if (!this.showEmptyImage && this.Release.Thumbnail == null)
                    {
                        return null;
                    }
                    if (this.image != null)
                    {
                        return this.image;
                    }

                    this.image = new System.Windows.Controls.Image();
                    this.image.Width = 48;
                    this.image.Height = 48;
                    this.image.Margin = new Thickness(0, 0, 3, 0);

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    if (this.Release.Thumbnail == null)
                    {
                        bitmap.UriSource = new Uri("/Images/JewelcaseNoImage.png", UriKind.RelativeOrAbsolute);
                    }
                    else
                    {
                        bitmap.StreamSource = new MemoryStream(this.Release.Thumbnail);
                    }
                    bitmap.EndInit();

                    this.image.Source = bitmap;

                    return this.image;
                }
            }

            public ReleaseItem(AlbumArtistItem albumArtistItem, Release release, bool showImage, bool showEmptyImage)
            {
                this.AlbumArtistItem = albumArtistItem;
                this.Release = release;
                this.showImage = showImage;
                this.showEmptyImage = showEmptyImage;
            }
        }

        private const int NumberOfReleasesToExpand = 3;

        private Dictionary<int, ReleaseItem> releaseItemByReleaseId = new Dictionary<int, ReleaseItem>();
        private SortedDictionary<string, SortedSet<Release>> releasesByAlbumArtist = new SortedDictionary<string, SortedSet<Release>>();
        private CollectionManager collectionManager;

        private ObservableCollection<AlbumArtistItem> albumArtists;

        public string[] AlbumArtists
        {
            get { return this.releasesByAlbumArtist.Keys.ToArray(); }
        }

        public CollectionManager CollectionManager
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

            this.albumArtists = new ObservableCollection<AlbumArtistItem>();
            this.treeListView.ItemsSource = this.albumArtists;
            this.textViewStatisticsHyperlink.Visibility = Visibility.Collapsed;
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
            this.albumArtists.Clear();

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

                    AlbumArtistItem albumArtistItem = new AlbumArtistItem()
                    {
                        Title = albumArtist.Key,
                        Releases = new List<ReleaseItem>()
                    };

                    foreach (Release release in albumArtist.Value)
                    {
                        ReleaseItem releaseItem = new ReleaseItem(albumArtistItem, release, showImages, showEmptyImage);
                        this.releaseItemByReleaseId[release.Id] = releaseItem;
                        albumArtistItem.Releases.Add(releaseItem);
                    }

                    albumArtists.Add(albumArtistItem);
                }
            }
        }

        public bool SetSelectedItem(object item)
        {
            if (item == null)
            {
                if (this.treeListView.SelectedItem != null)
                {
                    this.treeListView.SelectedItem = null;
                }
                return false;
            }
            else if (item is int)
            {
                int releaseId = (int)item;

                ReleaseItem releaseItem;
                if (this.releaseItemByReleaseId.TryGetValue(releaseId, out releaseItem))
                {
                    this.treeListView.ExpandHierarchyItem(releaseItem.AlbumArtistItem);
                    this.treeListView.SelectedItem = releaseItem;
                    return true;
                }

                return false;
            }
            else if (item is string)
            {
                string artistName = (string)item;
                foreach (AlbumArtistItem albumArtist in this.albumArtists)
                {
                    if (albumArtist.Title == artistName)
                    {
                        this.treeListView.SelectedItem = albumArtist;
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

        public object GetSelectedItem()
        {
            if (this.treeListView.SelectedItem == null)
            {
                return null;
            }
            else if (this.treeListView.SelectedItem is AlbumArtistItem)
            {
                AlbumArtistItem albumArtist = (AlbumArtistItem)this.treeListView.SelectedItem;
                return albumArtist.Title;
            }
            else if (this.treeListView.SelectedItem is ReleaseItem)
            {
                ReleaseItem releaseItem = (ReleaseItem)this.treeListView.SelectedItem;
                return releaseItem.Release.Id;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public int[] GetReleaseIdsByAlbumArtist(string albumArtist)
        {
            return this.releasesByAlbumArtist[albumArtist].Select(r => r.Id).ToArray();
        }

        private void treeListView_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            this.textViewStatisticsHyperlink.Visibility = this.treeListView.SelectedItem != null ? Visibility.Visible : Visibility.Collapsed;
            this.OnSelectedItemChanged();
        }

        private void hyperlinkViewStatistics_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            this.treeListView.SelectedItem = null;
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
