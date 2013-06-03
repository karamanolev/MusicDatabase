using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace MusicDatabase.Controls.ReleaseViews
{
    public class TreeArtistItem : INotifyPropertyChanged
    {
        private bool isSelected, isExpanded;

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

        public List<TreeReleaseItem> Releases { get; set; }

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
