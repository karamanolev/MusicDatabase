using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MusicDatabase.Engine.Entities;
using System.Windows.Media;
using MusicDatabase.Audio;
using System.Windows.Shapes;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for TracklistView.xaml
    /// </summary>
    public partial class TracklistView : UserControl
    {
        public class CustomListViewItem
        {
            public string Disc { get; set; }
            public string Position { get; set; }
            public string JoinedArtists { get; set; }
            public string Title { get; set; }
            public FontWeight FontWeight { get; set; }
            public FontStyle FontStyle { get; set; }

            public int DynamicRange { get; set; }
            public SolidColorBrush DynamicRangeBrush { get; set; }

            public string AlbumGain { get; set; }
            public string TrackGain { get; set; }
        }

        private static string FormatGain(double gain)
        {
            if (double.IsNaN(gain))
            {
                return "-";
            }
            return gain.ToString("0.00 dB");
        }

        private double drAlpha = 0.5;
        private Release[] releases;

        public Release[] Releases
        {
            get
            {
                return this.releases;
            }
            set
            {
                this.releases = value;
                this.UpdateUI();
            }
        }

        public TracklistView()
        {
            InitializeComponent();
        }

        private void UpdateUI()
        {
            this.listTracks.Items.Clear();

            if (this.releases == null)
            {
            }
            else if (this.releases.Length == 1)
            {

                this.columnArtists.Width = this.releases[0].HasTrackArtists ? 240 : 0;
                this.columnDisc.Width = this.releases[0].DiscCount != 1 ? 28 : 0;

                this.AddItemsForRelease(this.releases[0]);
            }
            else
            {
                this.columnArtists.Width = this.releases.Any(r => r.HasTrackArtists) ? 240 : 0;
                this.columnDisc.Width = this.releases.Any(r => r.DiscCount != 1) ? 28 : 0;

                foreach (Release release in this.releases)
                {
                    this.listTracks.Items.Add(new CustomListViewItem()
                    {
                        JoinedArtists = release.JoinedAlbumArtists,
                        Title = release.Title,
                        FontWeight = FontWeights.Bold,

                        DynamicRange = DspHelper.GetRoundedDr(release.DynamicRange),
                        DynamicRangeBrush = UIHelper.GetDrBrush(DspHelper.GetRoundedDr(release.DynamicRange), drAlpha),

                        AlbumGain = FormatGain(release.ReplayGainAlbumGain)
                    });

                    this.AddItemsForRelease(release);
                }
            }
        }

        private void AddItemsForRelease(Release release)
        {
            int currentDisc = 0;
            foreach (Track track in release.Tracklist)
            {
                if (release.DiscCount != 1 && track.Disc != currentDisc)
                {
                    currentDisc = track.Disc;
                    this.listTracks.Items.Add(new CustomListViewItem()
                    {
                        Title = "Disc " + currentDisc,
                        FontStyle = FontStyles.Italic,

                        DynamicRange = DspHelper.GetRoundedDr(release.DynamicRange),
                        DynamicRangeBrush = UIHelper.GetDrBrush(DspHelper.GetRoundedDr(release.DynamicRange), drAlpha),

                        AlbumGain = FormatGain(release.ReplayGainAlbumGain)
                    });
                }

                this.listTracks.Items.Add(new CustomListViewItem()
                {
                    Disc = track.Disc.ToString(),
                    Position = track.Position.ToString(),
                    JoinedArtists = track.JoinedArtists,
                    Title = track.Title,

                    DynamicRange = DspHelper.GetRoundedDr(track.DynamicRange),
                    DynamicRangeBrush = UIHelper.GetDrBrush(DspHelper.GetRoundedDr(release.DynamicRange), drAlpha),

                    AlbumGain = FormatGain(release.ReplayGainAlbumGain),
                    TrackGain = FormatGain(track.ReplayGainTrackGain)
                });
            }
        }
    }
}
