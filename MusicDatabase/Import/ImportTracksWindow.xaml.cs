using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Audio.Flac;
using MusicDatabase.Audio.Network;
using MusicDatabase.DiscogsLink;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.ImportExport;
using MusicDatabase.Engine.Tagging;
using MusicDatabase.WikipediaLink;
using MongoDB.Bson;
using MusicDatabase.Engine.Database;

namespace MusicDatabase.Import
{
    /// <summary>
    /// Interaction logic for ImportTracksWindow.xaml
    /// </summary>
    public partial class ImportTracksWindow : MusicDatabaseWindow
    {
        private bool completed;

        public string InsertedReleaseId { get; private set; }
        public ImportReleaseItem Release { get; private set; }
        public Release DatabaseRelease { get; private set; }

        public ImportTracksWindow(ICollectionSessionFactory collectionSessionFactory)
            : base(collectionSessionFactory)
        {
            this.DataContext = this;
            this.Release = new ImportReleaseItem();

            this.DatabaseRelease = new Release();

            InitializeComponent();

            this.detailsEditor.Release = this.DatabaseRelease;
            this.additionalFilesEditor.Release = this.DatabaseRelease;
            this.imagesEditor.Release = this.DatabaseRelease;
            this.imagesEditor.CollectionManager = this.CollectionManager;
            this.networkBox.CollectionManager = this.CollectionManager;
            this.networkBox.SettingsManager = this.SettingsManager;

            this.AddDisc();
        }

        public void LoadDataFromRelease(Release release, ICollectionImageHandler imageHandler)
        {
            if (release.DiscCount != this.DatabaseRelease.DiscCount || release.Tracklist.Count != this.DatabaseRelease.Tracklist.Count)
            {
                Dialogs.Error("Tracklists do not match. Can not continue.");
                return;
            }

            this.DatabaseRelease.AdditionalFiles.Clear();
            this.DatabaseRelease.AdditionalFiles.AddRange(release.AdditionalFiles);

            this.DatabaseRelease.Artists.Clear();
            this.DatabaseRelease.Artists.AddRange(release.Artists.Select(ra => new ReleaseArtist()
            {
                Artist = this.CollectionManager.GetOrCreateArtist(ra.Artist.Name),
                JoinString = ra.JoinString
            }));

            this.DatabaseRelease.CatalogNumber = release.CatalogNumber;
            this.DatabaseRelease.Country = release.Country;
            //this.DatabaseRelease.DateAdded = release.DateAdded;
            //this.DatabaseRelease.DateModified = release.DateModified;
            //this.DatabaseRelease.DateAudioModified = release.DateAudioModified;
            this.DatabaseRelease.DiscogsReleaseId = release.DiscogsReleaseId;
            this.DatabaseRelease.DiscogsMasterId = release.DiscogsMasterId;
            this.DatabaseRelease.FlagMessage = release.FlagMessage;
            this.DatabaseRelease.Genre = release.Genre;
            this.DatabaseRelease.IsFlagged = release.IsFlagged;
            this.DatabaseRelease.JoinedAlbumArtists = release.JoinedAlbumArtists;
            this.DatabaseRelease.Label = release.Label;
            this.DatabaseRelease.Notes = release.Notes;
            this.DatabaseRelease.WikipediaPageName = release.WikipediaPageName;
            this.DatabaseRelease.OriginalReleaseDate = release.OriginalReleaseDate;
            this.DatabaseRelease.ReleaseDate = release.ReleaseDate;
            this.DatabaseRelease.Title = release.Title;

            this.additionalFilesEditor.Release = null;
            this.additionalFilesEditor.Release = this.DatabaseRelease;

            this.DatabaseRelease.Images.Clear();
            this.imagesEditor.Release = null;
            this.imagesEditor.Release = this.DatabaseRelease;
            foreach (var image in release.Images)
            {
                var newImage = new MusicDatabase.Engine.Entities.Image()
                {
                    Description = image.Description,
                    Extension = image.Extension,
                    IsMain = image.IsMain,
                    MimeType = image.MimeType,
                    Type = image.Type
                };
                this.DatabaseRelease.Images.Add(newImage);
                this.imagesEditor.AddImage(newImage, imageHandler.LoadImage(image));
            }

            for (int i = 0; i < release.Tracklist.Count; ++i)
            {
                this.DatabaseRelease.Tracklist[i].Artists.Clear();
                this.DatabaseRelease.Tracklist[i].Artists.AddRange(release.Tracklist[i].Artists.Select(a => new TrackArtist()
                {
                    Artist = this.CollectionManager.GetOrCreateArtist(a.Artist.Name),
                    JoinString = a.JoinString
                }));
                this.DatabaseRelease.Tracklist[i].JoinedArtists = release.Tracklist[i].JoinedArtists;
                this.DatabaseRelease.Tracklist[i].Title = release.Tracklist[i].Title;
            }

            this.detailsEditor.Release = null;
            this.detailsEditor.Release = this.DatabaseRelease;
            this.RefreshAllDiscs();
        }

        private void MusicDatabaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!completed && !Dialogs.Confirm())
            {
                e.Cancel = true;
                return;
            }
        }

        private void btnAddDisc_Click(object sender, RoutedEventArgs e)
        {
            this.AddDisc();
        }

        private void AddDisc()
        {
            ObservableCollection<ImportTrackItem> disc = new ObservableCollection<ImportTrackItem>();
            this.Release.Discs.Add(disc);

            ImportTracksDiscEditor discEditor = new ImportTracksDiscEditor(this.CollectionManager)
            {
                Disc = this.Release.Discs.Count,
                Tracks = disc,
                Release = this.DatabaseRelease
            };
            discEditor.ItemAdding += new EventHandler<TrackAddingEventArgs>(discEditor_ItemAdding);

            this.tabs.Items.Add(new TabItem()
            {
                Header = "Disc " + this.Release.Discs.Count,
                Content = discEditor
            });
            this.tabs.SelectedIndex = this.tabs.Items.Count - 1;
        }

        private void btnRemoveDisc_Click(object sender, RoutedEventArgs e)
        {
            this.RemoveDisc();
        }

        private void RemoveDisc()
        {
            if (this.Release.Discs.Count > 1)
            {
                foreach (Track track in this.DatabaseRelease.Tracklist.Where(t => t.Disc == this.Release.Discs.Count).ToArray())
                {
                    this.DatabaseRelease.Tracklist.Remove(track);
                }

                this.Release.Discs.RemoveAt(this.Release.Discs.Count - 1);
                this.tabs.Items.RemoveAt(this.tabs.Items.Count - 1);
            }
        }

        private void discEditor_ItemAdding(object sender, TrackAddingEventArgs e)
        {
            this.UpdateRelease();

            string albumArtists = string.IsNullOrEmpty(e.ImportTrackItem.SourceItem.Tag.AlbumArtists) ?
                e.ImportTrackItem.SourceItem.Tag.Artists : e.ImportTrackItem.SourceItem.Tag.AlbumArtists;
            string artists = string.IsNullOrEmpty(e.ImportTrackItem.SourceItem.Tag.Artists) ?
                e.ImportTrackItem.SourceItem.Tag.AlbumArtists : e.ImportTrackItem.SourceItem.Tag.Artists;

            if (string.IsNullOrEmpty(this.DatabaseRelease.JoinedAlbumArtists) && !string.IsNullOrEmpty(albumArtists))
            {
                this.DatabaseRelease.Artists.Clear();
                this.DatabaseRelease.Artists.Add(new ReleaseArtist()
                {
                    Artist = this.CollectionManager.GetOrCreateArtist(albumArtists)
                });
                this.DatabaseRelease.JoinedAlbumArtists = albumArtists;
            }
            if (string.IsNullOrEmpty(this.DatabaseRelease.Title))
            {
                this.DatabaseRelease.Title = e.ImportTrackItem.SourceItem.Tag.Album;
            }
            if (this.DatabaseRelease.ReleaseDate.Type == ReleaseDateType.Invalid && e.ImportTrackItem.SourceItem.Tag.Year != 0)
            {
                this.DatabaseRelease.ReleaseDate = new ReleaseDate(e.ImportTrackItem.SourceItem.Tag.Year);
            }
            if (string.IsNullOrEmpty(this.DatabaseRelease.Genre))
            {
                this.DatabaseRelease.Genre = e.ImportTrackItem.SourceItem.Tag.Genre;
            }

            if (artists != this.DatabaseRelease.JoinedAlbumArtists && !string.IsNullOrEmpty(artists))
            {
                foreach (Track track in this.DatabaseRelease.Tracklist)
                {
                    if (string.IsNullOrEmpty(track.JoinedArtists))
                    {
                        track.Artists.Add(new TrackArtist()
                        {
                            Artist = this.CollectionManager.GetOrCreateArtist(artists)
                        });
                        track.JoinedArtists = artists;
                    }
                }

                e.ImportTrackItem.Track.Artists.Add(new TrackArtist()
                {
                    Artist = this.CollectionManager.GetOrCreateArtist(artists)
                });
                e.ImportTrackItem.Track.JoinedArtists = artists;
            }

            this.detailsEditor.Release = null;
            this.detailsEditor.Release = this.DatabaseRelease;
            this.RefreshAllDiscs();
        }

        private void OKCancelBox_OKClicked(object sender, System.EventArgs e)
        {
            this.UpdateRelease();

            IEncoderFactory encoderFactory;
            if (this.CollectionManager.Settings.NetworkEncoding)
            {
                encoderFactory = new RemoteFlacEncoderFactory(this.networkBox.Servers, 8, this.SettingsManager.Settings.ActualLocalConcurrencyLevel, true, true);
            }
            else
            {
                encoderFactory = new NativeFlacEncoderFactory(8, this.SettingsManager.Settings.ActualLocalConcurrencyLevel, true, true);
            }

            IEncoderFactory replayGainFactory = new DspEncoderFactory(this.SettingsManager.Settings.ActualLocalConcurrencyLevel, true, true);

            ReplayGainTask rgTask = new ReplayGainTask(replayGainFactory, this.DatabaseRelease, true, true, true);

            List<IParallelTask> tasks = new List<IParallelTask>();
            foreach (Track track in this.DatabaseRelease.Tracklist)
            {
                track.RelativeFilename = FilenameGenerator.PatternToFilename(
                        this.CollectionManager.Settings.FileNamingPattern,
                        this.DatabaseRelease, track) + ".flac";

                AudioFileTag tag = new AudioFileTag(this.DatabaseRelease, track);
                IImportSourceItem sourceItem = this.Release.Discs[track.Disc - 1][track.Position - 1].SourceItem;

                string filename = Path.Combine(this.CollectionManager.Settings.MusicDirectory, track.RelativeFilename);

                var task = new FileEncodeTask(
                    encoderFactory,
                    () => sourceItem.GetAudioSource(),
                    filename,
                    tag
                    );
                tasks.Add(task);
                rgTask.AddItem(track, task);
            }

            tasks.Add(rgTask);

            int concurrency = Math.Max(encoderFactory.ThreadCount, replayGainFactory.ThreadCount);
            EncoderController controller = new EncoderController(tasks.ToArray(), concurrency);
            EncodingWindow window = new EncodingWindow(controller);
            if (window.ShowDialog(this) == true)
            {
                this.DatabaseRelease.UpdateDynamicProperties();

                this.CollectionManager.Save(this.DatabaseRelease);
                this.InsertedReleaseId = this.DatabaseRelease.Id;

                this.imagesEditor.WriteFiles();


                CollectionManagerGlobal.OnCollectionChanged();

                this.completed = true;
                this.DialogResult = true;
            }
        }

        private void UpdateRelease()
        {
            this.detailsEditor.CommitChanges(this.CollectionManager);
            foreach (TabItem tab in this.tabs.Items)
            {
                ImportTracksDiscEditor discEditor = (ImportTracksDiscEditor)tab.Content;
                discEditor.CommitChanges(this.CollectionManager);
            }
            ThumbnailGenerator.UpdateReleaseThumbnail(this.DatabaseRelease, this.imagesEditor);
            this.DatabaseRelease.DateAdded = DateTime.Now;
            this.DatabaseRelease.DateModified = DateTime.Now;
            this.DatabaseRelease.DateAudioModified = DateTime.Now;
        }

        private void btnAddDisc_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void btnAddDisc_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            this.AddDisc();
            string[] droppedItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            ((ImportTracksDiscEditor)this.tabs.SelectedContent).AddItems(droppedItems);
        }

        private void RefreshAllDiscs()
        {
            foreach (TabItem tab in this.tabs.Items)
            {
                ImportTracksDiscEditor discEditor = (ImportTracksDiscEditor)tab.Content;
                ObservableCollection<ImportTrackItem> trackList = discEditor.Tracks;
                discEditor.Tracks = null;
                discEditor.Tracks = trackList;
            }
        }

        private void btnRenumberTracks_Click(object sender, RoutedEventArgs e)
        {
            int discNumber = 1;
            int trackNumber = 1;
            foreach (Track track in this.DatabaseRelease.Tracklist)
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
            if (this.DatabaseRelease.JoinedAlbumArtists != null)
            {
                this.DatabaseRelease.JoinedAlbumArtists = Utility.CapitalizeWords(this.DatabaseRelease.JoinedAlbumArtists);
            }

            if (this.DatabaseRelease.Title != null)
            {
                this.DatabaseRelease.Title = Utility.CapitalizeWords(this.DatabaseRelease.Title);
            }

            if (this.DatabaseRelease.Genre != null)
            {
                this.DatabaseRelease.Genre = Utility.CapitalizeWords(this.DatabaseRelease.Genre);
            }

            foreach (Track track in this.DatabaseRelease.Tracklist)
            {
                foreach (TrackArtist trackArtist in track.Artists)
                {
                    string capitalizedName = Utility.CapitalizeWords(trackArtist.Artist.Name);
                    if (capitalizedName != trackArtist.Artist.Name)
                    {
                        trackArtist.Artist = this.CollectionManager.GetOrCreateArtist(capitalizedName);
                    }
                }
                if (track.Artists.Count > 0)
                {
                    track.JoinedArtists = track.Artists.Join();
                }

                if (track.Title != null)
                {
                    track.Title = Utility.CapitalizeWords(track.Title);
                }
            }

            this.detailsEditor.Release = null;
            this.detailsEditor.Release = this.DatabaseRelease;
            this.RefreshAllDiscs();
        }

        private void btnDiscogsMatch_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateRelease();

            DiscogsReleaseMerger merger = new DiscogsReleaseMerger(this, this.CollectionManager, this.DatabaseRelease, this.imagesEditor);
            foreach (var disc in this.Release.Discs)
            {
                merger.AddDisc(disc.Select(t => new Tuple<Track, string>(t.Track, t.SourceItem.Name)));
            }
            merger.Merge();

            this.detailsEditor.Release = null;
            this.detailsEditor.Release = this.DatabaseRelease;
            this.RefreshAllDiscs();
        }

        private void btnWikipediaMatch_Click(object sender, RoutedEventArgs e)
        {
            this.UpdateRelease();

            WikipediaImporter wikiImporter = new WikipediaImporter(this, this.DatabaseRelease);
            if (wikiImporter.Import())
            {
                this.detailsEditor.Release = null;
                this.detailsEditor.Release = this.DatabaseRelease;
                this.RefreshAllDiscs();
            }
        }

        private void MusicDatabaseWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && Path.GetExtension(files[0]).ToLower() == ".xml")
                {
                    e.Effects = DragDropEffects.All;
                    e.Handled = true;
                    return;
                }
            }

            e.Effects = DragDropEffects.None;
        }

        private void MusicDatabaseWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && Path.GetExtension(files[0]).ToLower() == ".xml")
                {
                    this.OnXmlFileDropped(files[0]);

                    e.Effects = DragDropEffects.All;
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OnXmlFileDropped(string file)
        {
            if (this.DatabaseRelease.Images.Count != 0 || this.DatabaseRelease.AdditionalFiles.Count != 0)
            {
                if (!Dialogs.Confirm("This will replace all images, additional files and other data. Continue?"))
                {
                    return;
                }
            }

            WaitWindow waitWindow = new WaitWindow("Reading release data...");
            waitWindow.ShowDialog(this, () =>
            {
                var tempFactory = new MemorySessionFactory();
                var tempManager = tempFactory.CreateCollectionManager();

                using (XmlReleaseImporter xmlReleaseImporter = new XmlReleaseImporter(file, tempManager))
                {
                    Release release = xmlReleaseImporter.ImportRelease();

                    this.Dispatcher.InvokeAction(() =>
                    {
                        this.LoadDataFromRelease(release, tempManager.ImageHandler);

                        tempManager.Dispose();
                        tempFactory.Dispose();
                    });
                }
            });
        }
    }
}
