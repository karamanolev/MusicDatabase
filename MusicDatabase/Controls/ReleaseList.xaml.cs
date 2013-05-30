using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using System.Windows.Media;
using MusicDatabase.Audio;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ReleaseList.xaml
    /// </summary>
    public partial class ReleaseList : UserControl, IReleaseBrowser
    {
        public class ReleaseItem
        {
            private System.Windows.Controls.Image image;
            private bool showImage;

            public Release Release { get; private set; }

            public string Title
            {
                get { return Release.Title; }
            }

            public string Artists
            {
                get { return Release.JoinedAlbumArtists; }
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

            public int DynamicRange
            {
                get
                {
                    return DspHelper.GetRoundedDr(this.Release.DynamicRange);
                }
            }

            public SolidColorBrush DynamicRangeBrush
            {
                get
                {
                    return UIHelper.GetDrBrush(this.DynamicRange);
                }
            }

            public ReleaseItem(Release release, bool showImage)
            {
                this.showImage = showImage;
                this.Release = release;
            }
        }

        private Dictionary<int, ReleaseItem> releaseItemByReleaseId = new Dictionary<int, ReleaseItem>();

        public string[] AlbumArtists
        {
            get { return null; }

        }
        public CollectionManager CollectionManager { get; set; }

        public ReleaseList()
        {
            InitializeComponent();
        }

        private void gridView_SelectionChanged(object sender, Telerik.Windows.Controls.SelectionChangeEventArgs e)
        {
            this.OnSelectedItemChanged();
        }

        public int[] GetReleaseIdsByAlbumArtist(string albumArtist)
        {
            return null;
        }

        private int TieBreakCompare(Release a, Release b)
        {
            if (a.Id == b.Id)
            {
                return 0;
            }

            return (a.JoinedAlbumArtists + " - " + a.Title).CompareTo(
                b.JoinedAlbumArtists + " - " + b.Title);
        }

        private IComparer<Release> GetComparer()
        {
            if (this.CollectionManager.Settings.ReleasesViewMode == ReleasesViewMode.AlphabeticalList)
            {
                return new CallbackComparer<Release>((a, b) =>
                {
                    int result = a.Title.CompareTo(b.Title);
                    if (result == 0) return this.TieBreakCompare(a, b);
                    return result;
                });
            }
            else if (this.CollectionManager.Settings.ReleasesViewMode == ReleasesViewMode.ChronologicalList)
            {
                return new CallbackComparer<Release>((a, b) =>
                {
                    ReleaseDate aDate = a.OriginalReleaseDate.IsValid ? a.OriginalReleaseDate : a.ReleaseDate;
                    ReleaseDate bDate = b.OriginalReleaseDate.IsValid ? b.OriginalReleaseDate : b.ReleaseDate;
                    int result = bDate.Date.CompareTo(aDate.Date);
                    if (result == 0) return this.TieBreakCompare(a, b);
                    return result;
                });
            }
            else if (this.CollectionManager.Settings.ReleasesViewMode == ReleasesViewMode.DateModifiedList)
            {
                return new CallbackComparer<Release>((a, b) =>
                {
                    DateTime aModified = a.DateModified;
                    DateTime bModified = b.DateModified;
                    int result = bModified.CompareTo(aModified);
                    if (result == 0) return this.TieBreakCompare(a, b);
                    return result;
                });
            }
            else if (this.CollectionManager.Settings.ReleasesViewMode == ReleasesViewMode.DateAddedList)
            {
                return new CallbackComparer<Release>((a, b) =>
                {
                    DateTime aAdded = a.DateAdded;
                    DateTime bAdded = b.DateAdded;
                    int result = bAdded.CompareTo(aAdded);
                    if (result == 0) return this.TieBreakCompare(a, b);
                    return result;
                });
            }
            else
            {
                if (this.CollectionManager.Settings.ReleasesViewMode.ToString().EndsWith("List"))
                {
                    throw new NotImplementedException();
                }
                return null;
            }
        }

        public void LoadReleases(Release[] releases)
        {
            this.gridView.ItemsSource = null;
            this.releaseItemByReleaseId.Clear();

            if (releases != null)
            {
                bool showImages = this.CollectionManager.Settings.ShowImagesInReleaseTree;

                var releaseItems = releases.OrderBy(r => r, this.GetComparer()).Select(r => new ReleaseItem(r, showImages)).ToArray();

                this.releaseItemByReleaseId.Clear();
                foreach (var releaseItem in releaseItems)
                {
                    this.releaseItemByReleaseId[releaseItem.Release.Id] = releaseItem;
                }

                this.gridView.ItemsSource = releaseItems;
            }
        }

        public object GetSelectedItem()
        {
            ReleaseItem releaseItem = (ReleaseItem)this.gridView.SelectedItem;
            if (releaseItem == null)
            {
                return null;
            }
            return releaseItem.Release.Id;
        }

        public bool SetSelectedItem(object item)
        {
            if (item == null)
            {
                this.gridView.SelectedItem = null;
                return true;
            }
            else if (item is int)
            {
                ReleaseItem releaseItem;
                if (this.releaseItemByReleaseId.TryGetValue((int)item, out releaseItem))
                {
                    this.gridView.SelectedItem = releaseItem;

                    ReleaseItem[] items = (ReleaseItem[])this.gridView.ItemsSource;
                    int index = items.IndexOf(releaseItem);
                    this.gridView.ScrollIndexIntoView(Math.Max(0, index - 3));
                    this.gridView.ScrollIndexIntoView(Math.Min(items.Length - 1, index + 3));
                    this.gridView.ScrollIndexIntoView(index);

                    return true;
                }
                return false;
            }
            else if (item is string)
            {
                return false;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public event EventHandler SelectedItemChanged;
        private void OnSelectedItemChanged()
        {
            if (this.SelectedItemChanged != null)
            {
                this.SelectedItemChanged(this, EventArgs.Empty);
            }
        }

        private void hyperlinkViewStatistics_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            this.SetSelectedItem(null);
        }
    }
}
