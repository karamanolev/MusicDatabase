using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using MusicDatabase.Engine.Entities;
using System.ComponentModel;

namespace MusicDatabase.Controls.ReleaseViews
{
    public class TreeReleaseItem : INotifyPropertyChanged
    {
        private bool showImage;
        private bool showEmptyImage;
        private System.Windows.Controls.Image image;
        private bool isSelected, isExpanded;

        public TreeArtistItem ArtistItem { get; private set; }
        public Release Release { get; private set; }

        public bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                this.OnPropertyChanged("IsSelected");
            }
        }

        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                this.isExpanded = value;
                this.OnPropertyChanged("IsExpanded");
            }
        }

        public string Margin
        {
            get { return "-19,0,0,0"; }
        }

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

        public TreeReleaseItem(TreeArtistItem artistItem, Release release, bool showImage, bool showEmptyImage)
        {
            this.ArtistItem = artistItem;
            this.Release = release;
            this.showImage = showImage;
            this.showEmptyImage = showEmptyImage;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
