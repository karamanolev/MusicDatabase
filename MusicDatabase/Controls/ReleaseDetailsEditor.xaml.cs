using System.Windows.Controls;
using MusicDatabase.Engine.Entities;
using System.Windows;
using MusicDatabase.Engine;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ReleaseDetailsEditor.xaml
    /// </summary>
    public partial class ReleaseDetailsEditor : UserControl
    {
        public Release Release
        {
            get { return (Release)GetValue(ReleaseProperty); }
            set { SetValue(ReleaseProperty, value); }
        }
        public static readonly DependencyProperty ReleaseProperty =
            DependencyProperty.Register("Release", typeof(Release), typeof(ReleaseDetailsEditor), new UIPropertyMetadata(null, OnReleasePropertyChangedCallback));
        private static void OnReleasePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ReleaseDetailsEditor)sender).OnReleaseChanged();
        }

        public ReleaseDetailsEditor()
        {
            InitializeComponent();
        }

        private void OnReleaseChanged()
        {
            this.DataContext = this.Release;

            if (this.Release == null)
            {
                this.textDiscogsRelease.Text = "";
                this.textReleaseDate.Text = "";
            }
            else
            {
                this.textDiscogsRelease.Text = this.Release.DiscogsReleaseId == 0 ? "" : this.Release.DiscogsReleaseId.ToString();
                this.textDiscogsMaster.Text = this.Release.DiscogsMasterId == 0 ? "" : this.Release.DiscogsMasterId.ToString();
                this.textReleaseDate.Text = this.Release.ReleaseDate.ToString();
                this.textOriginalReleaseDate.Text = this.Release.OriginalReleaseDate.ToString();
                this.textWikipediaPageName.Text = this.Release.WikipediaPageName;
            }
        }

        public void CommitChanges(ICollectionManager collectionManager)
        {
            if (this.Release.Artists.Join() != this.Release.JoinedAlbumArtists)
            {
                this.Release.Artists.Clear();
                this.Release.Artists.Add(new ReleaseArtist()
                {
                    Artist = collectionManager.GetOrCreateArtist(this.Release.JoinedAlbumArtists)
                });
            }

            this.Release.ReleaseDate = ReleaseDate.Parse(this.textReleaseDate.Text);
            this.Release.OriginalReleaseDate = ReleaseDate.Parse(this.textOriginalReleaseDate.Text);

            int discogsRelease, discogsMaster;
            int.TryParse(this.textDiscogsRelease.Text, out discogsRelease);
            int.TryParse(this.textDiscogsMaster.Text, out discogsMaster);
            this.Release.DiscogsReleaseId = discogsRelease;
            this.Release.DiscogsMasterId = discogsMaster;
            this.Release.WikipediaPageName = this.textWikipediaPageName.Text;
        }
    }
}
