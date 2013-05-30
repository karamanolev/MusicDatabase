using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DiscogsNet;
using DiscogsNet.Model;
using MusicDatabase.Engine;
using TrackEntity = MusicDatabase.Engine.Entities.Track;
using System;

namespace MusicDatabase.DiscogsLink
{
    /// <summary>
    /// Interaction logic for DiscogsMatchWindow.xaml
    /// </summary>
    public partial class DiscogsMatchWindow : MusicDatabaseWindow
    {
        class MatchItem : INotifyPropertyChanged
        {
            private string filename;
            public string Filename
            {
                get
                {
                    return this.filename;
                }
                set
                {
                    this.filename = value;
                    this.OnPropertyChanged("Filename");
                }
            }

            public TrackEntity Item { get; set; }
            public Track Source { get; set; }

            private int track;
            public int Track
            {
                get
                {
                    return track;
                }
                set
                {
                    track = value;
                    OnPropertyChanged("Track");
                    OnPropertyChanged("TrackDisplay");
                }
            }

            public string TrackDisplay
            {
                get
                {
                    return track == 0 ? "" : track.ToString();
                }
            }

            private string artist;
            public string Artist
            {
                get
                {
                    return artist;
                }
                set
                {
                    artist = value;
                    OnPropertyChanged("Artist");
                }
            }

            private string title;
            public string Title
            {
                get
                {
                    return title;
                }
                set
                {
                    title = value;
                    OnPropertyChanged("Title");
                }
            }

            public MatchItem(TrackEntity editItem, string filename)
            {
                this.Item = editItem;
                this.Filename = filename;
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        private CollectionManager collectionManager;
        private Release release;
        private MatchItem[] matchItems;
        private int matchedItemsCount = 0;

        public bool FixNames { get; set; }

        public DiscogsMatchWindow(CollectionManager manager, IEnumerable<Tuple<TrackEntity, string>> tracks, Release release)
        {
            this.collectionManager = manager;
            this.FixNames = true;

            this.release = release;
            this.matchItems = tracks.Select(i => new MatchItem(i.Item1, i.Item2)).ToArray();

            InitializeComponent();

            this.listReleaseTracklist.DataContext = release;
            this.listReleaseTracklist.ItemsSource = release.Tracklist;

            this.listMatching.ItemsSource = matchItems;

            for (int i = 0; i < this.listReleaseTracklist.Items.Count; ++i)
            {
                Track t = (Track)this.listReleaseTracklist.Items[i];
                if (this.IsValidTrack(t))
                {
                    this.listReleaseTracklist.SelectedItems.Add(t);
                }
            }
        }

        private bool IsValidTrack(Track t)
        {
            return !string.IsNullOrEmpty(t.Position);
        }

        private void UpdateMatching()
        {
            Track[] tracks = listReleaseTracklist.SelectedItems.Cast<Track>().ToArray();
            matchedItemsCount = 0;
            int j = 0;
            for (int i = 0; i < matchItems.Length; ++i)
            {
                if (j >= tracks.Length)
                {
                    for (int k = i; k < matchItems.Length; ++k)
                    {
                        matchItems[k].Track = 0;
                        matchItems[k].Artist = null;
                        matchItems[k].Title = null;
                    }
                    break;
                }

                matchItems[i].Track = i + 1;
                matchItems[i].Source = tracks[j];

                if (this.FixNames)
                {
                    matchItems[i].Artist = string.IsNullOrEmpty(tracks[j].Aggregate.JoinedArtistsFixed) ? release.Aggregate.JoinedArtistsFixed : tracks[j].Aggregate.GetJoinedArtistsFixed(release);
                }
                else
                {
                    matchItems[i].Artist = string.IsNullOrEmpty(tracks[j].Aggregate.JoinedArtistsFixed) ? release.Aggregate.JoinedArtists : tracks[j].Aggregate.GetJoinedArtistsFixed(release);
                }

                matchItems[i].Title = tracks[j].Title;
                ++matchedItemsCount;
                ++j;
            }
        }

        private void listReleaseTracklist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMatching();
        }

        private void checkFixNames_Checked(object sender, RoutedEventArgs e)
        {
            UpdateMatching();
        }

        private void checkFixNames_Unchecked(object sender, RoutedEventArgs e)
        {
            UpdateMatching();
        }

        private void Merge()
        {
            foreach (MatchItem matchItem in matchItems)
            {
                matchItem.Item.Position = matchItem.Track;

                ReleaseArtist[] releaseArtists;
                if (matchItem == null || matchItem.Source == null || matchItem.Source.Artists == null)
                {
                    releaseArtists = new ReleaseArtist[0];
                } else {
                    releaseArtists = matchItem.Source.Artists;
                }

                bool matchNames = Enumerable.SequenceEqual(
                        matchItem.Item.Artists.Select(a => a.Artist.Name),
                        releaseArtists.Select(a => a.Aggregate.NameVariationFixed));
                bool matchJoins = Enumerable.SequenceEqual(
                        matchItem.Item.Artists.Select(a => a.JoinString),
                        releaseArtists.Select(a => a.Join));

                if (!matchNames || !matchJoins)
                {
                    matchItem.Item.Artists.Clear();
                    foreach (ReleaseArtist releaseArtist in releaseArtists)
                    {
                        matchItem.Item.Artists.Add(new Engine.Entities.TrackArtist()
                        {
                            Artist = this.collectionManager.GetOrCreateArtist(releaseArtist.Aggregate.NameVariationFixed),
                            JoinString = releaseArtist.Join
                        });
                    }
                    matchItem.Item.JoinedArtists = releaseArtists.JoinFixed();
                }

                matchItem.Item.Title = matchItem.Title;
            }
        }

        private void OKCancelBox_OKClicked(object sender, System.EventArgs e)
        {
            this.Merge();

            this.DialogResult = true;
            this.Close();
        }
    }
}
