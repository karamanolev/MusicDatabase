using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.WikipediaLink;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ReleaseDetailsView.xaml
    /// </summary>
    public partial class ReleaseDetailsView : UserControl
    {
        private Release release;

        public Release Release
        {
            get
            {
                return this.release;
            }
            set
            {
                this.release = value;
                this.UpdateUI();
            }
        }

        public bool ShowControls
        {
            get
            {
                return this.btnToggleButtons.Visibility == Visibility.Visible;
            }
            set
            {
                this.btnFlagToggleSmall.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                this.btnPlay.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                this.btnToggleButtons.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public ICollectionManager CollectionManager { get; set; }

        public ReleaseDetailsView()
        {
            InitializeComponent();

            this.Release = null;
            this.UpdateButtons();
        }

        private void UpdateUI()
        {
            if (this.release == null)
            {
                this.image.ImageBytes = null;
                this.image.FullImageLazy = null;
                this.image.Visibility = Visibility.Collapsed;

                this.textArtistsTitle.Text = "No Release Selected";
                this.textGenre.Text = "";
                this.textLabel.Text = "";
                this.textCatalogNumber.Text = "";
            }
            else
            {
                this.image.Visibility = Visibility.Visible;
                if (this.release.Thumbnail != null)
                {
                    this.image.ImageBytes = this.release.Thumbnail;
                    this.image.FullImageLazy = () =>
                    {
                        if (this.release.MainImage != null)
                        {
                            return this.CollectionManager.ImageHandler.LoadImage(this.release.MainImage);
                        }
                        return null;
                    };
                }
                else
                {
                    this.image.ImageBytes = null;
                    this.image.FullImageLazy = null;
                }

                this.textArtistsTitle.Text = this.release.JoinedAlbumArtists + " - " + this.release.Title;
                ToolTipService.SetToolTip(this.textArtistsTitle,
                    "Added: " + this.release.DateAdded.ToString(Utility.DateTimeFormatString) + Environment.NewLine +
                    "Audio Modified: " + this.release.DateAudioModified.ToString(Utility.DateTimeFormatString) + Environment.NewLine +
                    "Modified: " + this.release.DateModified.ToString(Utility.DateTimeFormatString));

                this.textGenre.Text = this.release.Genre;
                this.textLabel.Text = this.release.Label;
                this.textCatalogNumber.Text = this.release.CatalogNumber;
                this.textCountry.Text = this.release.Country;

                if (this.release.OriginalReleaseDate.IsValid && !this.release.OriginalReleaseDate.Equals(this.release.ReleaseDate))
                {
                    this.textReleaseDate.Text = this.release.ReleaseDate + " (originally " + this.release.OriginalReleaseDate + ")";
                }
                else
                {
                    this.textReleaseDate.Text = this.release.ReleaseDate.ToString();
                }

                if (this.release.DiscogsReleaseId == 0)
                {
                    this.labelDiscogs.Visibility = Visibility.Collapsed;
                    this.textDiscogs.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.labelDiscogs.Visibility = Visibility.Visible;
                    this.textDiscogs.Visibility = Visibility.Visible;
                    this.linkDiscogs.NavigateUri = new Uri("http://www.discogs.com/release/" + this.release.DiscogsReleaseId);
                    this.textDiscogsHyperlink.Text = this.release.DiscogsReleaseId.ToString();
                }

                if (this.release.DiscogsMasterId == 0)
                {
                    this.labelDiscogsMaster.Visibility = Visibility.Collapsed;
                    this.textDiscogsMaster.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.labelDiscogsMaster.Visibility = Visibility.Visible;
                    this.textDiscogsMaster.Visibility = Visibility.Visible;
                    this.linkDiscogsMaster.NavigateUri = new Uri("http://www.discogs.com/master/" + this.release.DiscogsMasterId);
                    this.textDiscogsMasterHyperlink.Text = this.release.DiscogsMasterId.ToString();
                }

                if (string.IsNullOrEmpty(this.release.WikipediaPageName))
                {
                    this.labelWiki.Visibility = Visibility.Collapsed;
                    this.textWiki.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.labelWiki.Visibility = Visibility.Visible;
                    this.textWiki.Visibility = Visibility.Visible;
                    this.linkWiki.NavigateUri = new Uri(WikipediaImporter.MakeUrlFromPageName(this.release.WikipediaPageName));
                    this.textWikiHyperlink.Text = this.release.WikipediaPageName;
                }

                if (this.release.TorrentFile == null)
                {
                    this.labelTorrent.Visibility = Visibility.Collapsed;
                    this.torrentBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.labelTorrent.Visibility = Visibility.Visible;
                    this.torrentBox.Visibility = Visibility.Visible;
                    this.torrentBox.SetFile(this.release.TorrentFile.OriginalFilename, this.release.TorrentFile.File);
                }

                this.btnViewImages.IsEnabled = this.release.Images.Count > 0;
                this.btnViewFiles.IsEnabled = this.release.AdditionalFiles.Count > 0;

                if (this.release.IsFlagged)
                {
                    this.btnFlagToggleSmall.Visibility = Visibility.Visible;
                    this.btnFlagToggle.Icon = new IconExtension("FlagRed_16").ProvideValue(null) as System.Windows.Controls.Image;
                    this.labelFlagMessage.Visibility = Visibility.Visible;
                    this.textFlagMessage.Visibility = Visibility.Visible;
                    this.textFlagMessage.Text = this.release.FlagMessage;
                }
                else
                {
                    this.btnFlagToggleSmall.Visibility = Visibility.Collapsed;
                    this.btnFlagToggle.Icon = new IconExtension("FlagGreen_16").ProvideValue(null) as System.Windows.Controls.Image;
                    this.labelFlagMessage.Visibility = Visibility.Collapsed;
                    this.textFlagMessage.Visibility = Visibility.Collapsed;
                }

                this.releaseScore.Score = this.release.Score;
            }
        }

        private void link_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            this.OnEditReleaseClicked();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            this.OnDeleteReleaseClicked();
        }

        private void btnViewImages_Click(object sender, RoutedEventArgs e)
        {
            ViewReleaseImagesWindow viewImagesWindow = new ViewReleaseImagesWindow(this.CollectionManager, this.release);
            viewImagesWindow.ShowDialog(Window.GetWindow(this));
        }

        private void btnViewFiles_Click(object sender, RoutedEventArgs e)
        {
            ViewReleaseFilesWindow viewFilesWindow = new ViewReleaseFilesWindow(this.release);
            viewFilesWindow.ShowDialog(Window.GetWindow(this));
        }

        private void btnFlagToggle_Click(object sender, RoutedEventArgs e)
        {
            this.OnToggleReleaseFlagClicked();
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            this.OnPlayClicked();
        }

        private void toggleButtons_CheckedChanged(object sender, EventArgs e)
        {
            this.UpdateButtons();
        }

        private void UpdateButtons()
        {
            this.popup.IsOpen = this.btnToggleButtons.IsChecked == true;
        }

        private void popup_Closed(object sender, EventArgs e)
        {
            this.btnToggleButtons.IsChecked = this.popup.IsOpen;
        }

        private void btnChecksum_Click(object sender, RoutedEventArgs e)
        {
            this.OnChecksumClicked();
        }

        private void btnExplore_Click(object sender, RoutedEventArgs e)
        {
            this.OnExploreClicked();
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            this.OnRemoveReleaseClicked();
        }

        public event EventHandler EditReleaseClicked;
        private void OnEditReleaseClicked()
        {
            if (this.EditReleaseClicked != null)
            {
                this.EditReleaseClicked(this, EventArgs.Empty);
            }
        }

        public event EventHandler DeleteReleaseClicked;
        private void OnDeleteReleaseClicked()
        {
            if (this.DeleteReleaseClicked != null)
            {
                this.DeleteReleaseClicked(this, EventArgs.Empty);
            }
        }

        public event EventHandler RemoveReleaseClicked;
        private void OnRemoveReleaseClicked()
        {
            if (this.RemoveReleaseClicked != null)
            {
                this.RemoveReleaseClicked(this, EventArgs.Empty);
            }
        }

        public event EventHandler ToggleReleaseFlagClicked;
        private void OnToggleReleaseFlagClicked()
        {
            if (this.ToggleReleaseFlagClicked != null)
            {
                this.ToggleReleaseFlagClicked(this, EventArgs.Empty);
            }
        }

        public event EventHandler PlayClicked;
        private void OnPlayClicked()
        {
            if (this.PlayClicked != null)
            {
                this.PlayClicked(this, EventArgs.Empty);
            }
        }

        public event EventHandler ChecksumClicked;
        private void OnChecksumClicked()
        {
            if (this.ChecksumClicked != null)
            {
                this.ChecksumClicked(this, EventArgs.Empty);
            }
        }

        public event EventHandler ExploreClicked;
        private void OnExploreClicked()
        {
            if (this.ExploreClicked != null)
            {
                this.ExploreClicked(this, EventArgs.Empty);
            }
        }
    }
}
