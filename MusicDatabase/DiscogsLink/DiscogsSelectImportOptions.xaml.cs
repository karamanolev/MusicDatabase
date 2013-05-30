using System;
using System.Windows;

namespace MusicDatabase.DiscogsLink
{
    /// <summary>
    /// Interaction logic for DiscogsSelectImportOptions.xaml
    /// </summary>
    public partial class DiscogsSelectImportOptions : MusicDatabaseWindow
    {
        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public bool ShowImportTitle
        {
            get { return this.checkImportTitle.Visibility == Visibility.Visible; }
            set { this.checkImportTitle.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public bool ImportTitle
        {
            get { return this.checkImportTitle.IsChecked == true; }
            set { this.checkImportTitle.IsChecked = value; }
        }

        public bool ShowImportTrackData
        {
            get { return this.checkImportTrackData.Visibility == Visibility.Visible; }
            set { this.checkImportTrackData.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public bool ImportTrackData
        {
            get { return this.checkImportTrackData.IsChecked == true; }
            set { this.checkImportTrackData.IsChecked = value; }
        }

        public bool ShowImportAlbumArtist
        {
            get { return this.checkImportAlbumArtist.Visibility == Visibility.Visible; }
            set { this.checkImportAlbumArtist.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public bool ImportAlbumArtist
        {
            get { return this.checkImportAlbumArtist.IsChecked == true; }
            set { this.checkImportAlbumArtist.IsChecked = value; }
        }

        public bool ShowImportReleaseDate
        {
            get { return this.checkImportReleaseDate.Visibility == Visibility.Visible; }
            set { this.checkImportReleaseDate.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public bool ImportReleaseDate
        {
            get { return this.checkImportReleaseDate.IsChecked == true; }
            set { this.checkImportReleaseDate.IsChecked = value; }
        }

        public bool ShowImportImages
        {
            get { return this.checkImportImages.Visibility == Visibility.Visible; }
            set { this.checkImportImages.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public bool ImportImages
        {
            get { return this.checkImportImages.IsChecked == true; }
            set { this.checkImportImages.IsChecked = value; }
        }

        public bool ShowImportCatalogInformation
        {
            get { return this.checkImportCatalogInformation.Visibility == Visibility.Visible; }
            set { this.checkImportCatalogInformation.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public bool ImportCatalogInformation
        {
            get { return this.checkImportCatalogInformation.IsChecked == true; }
            set { this.checkImportCatalogInformation.IsChecked = value; }
        }

        public bool ShowImportNotes
        {
            get { return this.checkImportNotes.Visibility == Visibility.Visible; }
            set { this.checkImportNotes.Visibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }
        public bool ImportNotes
        {
            get { return this.checkImportNotes.IsChecked == true; }
            set { this.checkImportNotes.IsChecked = value; }
        }

        public bool FixNames
        {
            get { return this.checkFixNames.IsChecked == true; }
        }

        public DiscogsSelectImportOptions()
        {
            InitializeComponent();
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
