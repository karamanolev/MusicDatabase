using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MusicDatabase.DiscogsLink;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Tagging;
using MusicDatabase.WikipediaLink;

namespace MusicDatabase.Edit
{
    /// <summary>
    /// Interaction logic for EditReleaseWindow.xaml
    /// </summary>
    public partial class EditReleaseWindow : MusicDatabaseWindow
    {
        private bool completed;
        private Release release;

        public EditReleaseWindow(ICollectionSessionFactory sessionFactory, int releaseId)
            : base(sessionFactory)
        {
            this.release = this.CollectionManager.Releases.Where(r => r.Id == releaseId).FirstOrDefault();

            InitializeComponent();

            this.detailsEditor.Release = this.release;
            this.imagesEditor.CollectionManager = this.CollectionManager;
            this.imagesEditor.Release = this.release;
            this.additionalFilesEditor.Release = this.release;

            for (int i = 1; i <= this.release.DiscCount; ++i)
            {
                EditReleaseDiscEditor discEditor = new EditReleaseDiscEditor();
                discEditor.SetData(this.release, i);

                this.tabs.Items.Add(new TabItem()
                {
                    Header = "Disc " + i,
                    Content = discEditor
                });
            }
        }

        private void RefreshAllDiscs()
        {
            int discNumber = 1;
            foreach (TabItem tab in this.tabs.Items)
            {
                EditReleaseDiscEditor discEditor = (EditReleaseDiscEditor)tab.Content;
                discEditor.SetData(this.release, discNumber);
                discNumber++;
            }
        }

        private void UpdateRelease()
        {
            this.detailsEditor.CommitChanges(this.CollectionManager);

            bool hasTrackArtists = this.release.Tracklist.Any(t => !string.IsNullOrEmpty(t.JoinedArtists));
            foreach (Track track in this.release.Tracklist)
            {
                if (hasTrackArtists)
                {
                    if (track.Artists.Join() != track.JoinedArtists)
                    {
                        track.Artists.Clear();
                        track.Artists.Add(new TrackArtist()
                        {
                            Artist = CollectionManager.GetOrCreateArtist(track.JoinedArtists)
                        });
                    }
                }
                else
                {
                    track.Artists.Clear();
                }
            }

            UIHelper.UpdateReleaseThumbnail(this.release, this.imagesEditor);

            this.release.DateModified = DateTime.Now;
        }

        private void btnRenumberTracks_Click(object sender, RoutedEventArgs e)
        {
            int discNumber = 1;
            int trackNumber = 1;
            foreach (Track track in this.release.Tracklist)
            {
                if (track.Disc != discNumber)
                {
                    discNumber = 1;
                    trackNumber = 1;
                }

                track.Position = trackNumber;
                ++trackNumber;
            }

            this.RefreshAllDiscs();
        }

        private void btnCapitalizeTitles_Click(object sender, RoutedEventArgs e)
        {
            this.release.JoinedAlbumArtists = Utility.CapitalizeWords(this.release.JoinedAlbumArtists);
            this.release.Title = Utility.CapitalizeWords(this.release.Title);
            this.release.Genre = Utility.CapitalizeWords(this.release.Genre);

            foreach (Track track in this.release.Tracklist)
            {
                foreach (TrackArtist trackArtist in track.Artists)
                {
                    string capitalizedName = Utility.CapitalizeWords(trackArtist.Artist.Name);
                    if (capitalizedName != trackArtist.Artist.Name)
                    {
                        trackArtist.Artist = this.CollectionManager.GetOrCreateArtist(capitalizedName);
                    }
                }

                track.Title = Utility.CapitalizeWords(track.Title);
            }

            this.detailsEditor.Release = null;
            this.detailsEditor.Release = this.release;
            this.RefreshAllDiscs();
        }

        private void btnDiscogsMatch_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateRelease();

            DiscogsReleaseMerger merger = new DiscogsReleaseMerger(this, this.CollectionManager, this.release, this.imagesEditor);
            for (int i = 1; i <= this.release.DiscCount; ++i)
            {
                var tracks = this.release.Tracklist.Where(t => t.Disc == i);
                var discogsItems = tracks.Select(t => new Tuple<Track, string>(t, Path.GetFileName(t.RelativeFilename)));
                merger.AddDisc(discogsItems.ToArray());
            }
            merger.Merge();

            this.detailsEditor.Release = null;
            this.detailsEditor.Release = this.release;
            this.RefreshAllDiscs();
        }

        private void btnWikipediaMatch_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateRelease();

            WikipediaImporter importer = new WikipediaImporter(this, this.release);
            if (importer.Import())
            {
                this.detailsEditor.Release = null;
                this.detailsEditor.Release = this.release;
                this.RefreshAllDiscs();
            }
        }

        private void OKCancelBox_OKClicked(object sender, System.EventArgs e)
        {
            this.UpdateRelease();

            WaitWindow waitWindow = new WaitWindow("Moving files and saving tags...");

            Progress<double> progress = new Progress<double>();

            waitWindow.ShowDialog(this, new Task(() =>
            {
                this.SaveChangesAsync(progress);
            }), progress);
        }

        private void SaveChangesAsync(IProgress<double> progress)
        {
            try
            {
                MoveTracksAsync();

                int tracksFinished = 0;
                foreach (Track track in this.release.Tracklist)
                {
                    AudioFileTag tag = new AudioFileTag(this.release, track);
                    string filename = Path.Combine(this.CollectionManager.Settings.MusicDirectory, track.RelativeFilename);
                    tag.WriteToFile(filename);

                    ++tracksFinished;
                    progress.Report((double)tracksFinished / this.release.Tracklist.Count);
                }

                using (var transaction = this.CollectionManager.BeginTransaction())
                {
                    release.UpdateDynamicProperties();
                    this.CollectionManager.SaveOrUpdate(release);

                    this.imagesEditor.WriteFiles();

                    transaction.Commit();
                }

                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.completed = true;
                    this.Close();
                }));
            }
            catch (Exception ex)
            {
                Utility.WriteToErrorLog(ex.ToString());
                Dialogs.Error("There was an error moving. Please try not to continue!");
            }
            finally
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.detailsEditor.Release = null;
                    this.detailsEditor.Release = this.release;
                    this.RefreshAllDiscs();

                    CollectionManager.OnCollectionChanged();
                }));
            }
        }

        private void MoveTracksAsync()
        {
            foreach (Track track in this.release.Tracklist)
            {
                string oldFilename = Path.Combine(this.CollectionManager.Settings.MusicDirectory, track.RelativeFilename);
                string newRelativeName = FilenameGenerator.PatternToFilename(
                    this.CollectionManager.Settings.FileNamingPattern, this.release, track) + ".flac";
                string newFilename = Path.Combine(this.CollectionManager.Settings.MusicDirectory, newRelativeName);

                while (true)
                {
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(newFilename));
                        File.Move(oldFilename, newFilename);
                        Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(oldFilename));
                        break;
                    }
                    catch (Exception moveException)
                    {
                        var result = Dialogs.YesNoCancel("Moving " + Path.GetFileName(oldFilename) + " failed. Retry / fail / continue.");
                        if (result == MessageBoxResult.Yes)
                        {
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            throw moveException;
                        }
                        else if (result == MessageBoxResult.Cancel)
                        {
                            break;
                        }
                    }
                }

                track.RelativeFilename = newRelativeName;

                using (var transaction = this.CollectionManager.BeginTransaction())
                {
                    this.CollectionManager.SaveOrUpdate(track);
                    transaction.Commit();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.completed && !Dialogs.Confirm())
            {
                e.Cancel = true;
            }
        }
    }
}
