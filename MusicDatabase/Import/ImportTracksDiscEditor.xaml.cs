using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MusicDatabase.Audio;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Import
{
    public partial class ImportTracksDiscEditor : UserControl
    {
        private CollectionManager collectionManager;

        public int Disc { get; set; }

        public Release Release { get; set; }

        public ObservableCollection<ImportTrackItem> Tracks
        {
            get
            {
                return (ObservableCollection<ImportTrackItem>)this.dataGrid.ItemsSource;
            }
            set
            {
                if (this.dataGrid.ItemsSource != null)
                {
                    ((ObservableCollection<ImportTrackItem>)this.dataGrid.ItemsSource).CollectionChanged -= this.value_CollectionChanged;
                }
                this.dataGrid.ItemsSource = value;
                if (value != null)
                {
                    value.CollectionChanged += value_CollectionChanged;
                }
            }
        }

        void value_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            foreach (Track track in this.Release.Tracklist.Where(t => t.Disc == this.Disc).ToArray())
            {
                if (!this.Tracks.Select(t => t.Track).Contains(track))
                {
                    this.Release.Tracklist.Remove(track);
                }
            }
        }

        public ImportTracksDiscEditor(CollectionManager collectionManager)
        {
            this.collectionManager = collectionManager;
            this.DataContext = this;

            InitializeComponent();
        }

        private void DataGrid_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            string[] droppedItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.AddItems(droppedItems);
        }

        public void AddItems(string[] items)
        {
            List<string> files = new List<string>();

            Action<string> addDirectory = null;
            addDirectory = directory =>
            {
                foreach (string subDirectory in Directory.GetDirectories(directory))
                {
                    addDirectory(subDirectory);
                }
                foreach (string file in Directory.GetFiles(directory))
                {
                    files.Add(file);
                }
            };

            foreach (string item in items)
            {
                if (File.Exists(item))
                {
                    files.Add(item);
                }
                else if (Directory.Exists(item))
                {
                    addDirectory(item);
                }
            }

            files.Sort();

            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ".cue")
                {
                    this.AddCue(file);
                }
                else if (AudioHelper.IsSupportedAudioSource(file))
                {
                    this.AddAudioFile(file);
                }
            }
        }

        private void AddCue(string file)
        {
            CueSheet sheet;
            try
            {
                sheet = new CueSheet(file);
            }
            catch (Exception e)
            {
                Utility.WriteToErrorLog(e.ToString());
                Dialogs.Error("Error reading cue file. Invalid format.");
                return;
            }

            bool isOriginal;
            string targetFilename = sheet.DiscoverTarget(out isOriginal);

            if (targetFilename == null)
            {
                Dialogs.Error("Cue target was not found!");
                return;
            }
            else if (!isOriginal)
            {
                Dialogs.Inform("Cue target was not found, but successfully discovered.");
            }

            if (!AudioHelper.IsSupportedAudioSource(targetFilename))
            {
                Dialogs.Error("Unsupported audio source!");
                return;
            }

            for (int i = 0; i < sheet.Tracks.Length; ++i)
            {
                CueSourceItem item = new CueSourceItem(targetFilename, sheet, i);
                this.AddItem(item);
            }
        }

        private void AddAudioFile(string file)
        {
            FileSourceItem item = new FileSourceItem(file);
            this.AddItem(item);
        }

        private void AddItem(IImportSourceItem importSourceItem)
        {
            Track track = new Track()
            {
                Disc = this.Disc,
                Position = this.Tracks.Count + 1,
                Title = importSourceItem.Tag.Title
            };
            ImportTrackItem importTrackItem = new ImportTrackItem(track, importSourceItem);

            this.OnItemAdding(importTrackItem);

            this.Tracks.Add(importTrackItem);
            int index = this.Release.Tracklist.LastIndexWhere(t => t.Disc <= this.Disc);
            this.Release.Tracklist.Insert(index + 1, track); // Will be inserted at 0 if there are no matching tracks.
        }

        public void CommitChanges(CollectionManager collectionManager)
        {
            bool hasTrackArtists = this.Release.Tracklist.Any(t => !string.IsNullOrEmpty(t.JoinedArtists));
            foreach (ImportTrackItem track in this.Tracks)
            {
                if (hasTrackArtists)
                {
                    if (track.Track.Artists.Join() != track.Track.JoinedArtists)
                    {
                        if (string.IsNullOrEmpty(track.Track.JoinedArtists))
                        {
                            track.Track.Artists.Clear();
                            track.Track.Artists.AddRange(this.Release.Artists.Select(releaseArtist => new TrackArtist()
                            {
                                Artist = releaseArtist.Artist,
                                JoinString = releaseArtist.JoinString
                            }));
                            track.Track.JoinedArtists = Release.JoinedAlbumArtists;
                        }
                        else
                        {
                            track.Track.Artists.Clear();
                            track.Track.Artists.Add(new TrackArtist()
                            {
                                Artist = collectionManager.GetOrCreateArtist(track.Track.JoinedArtists)
                            });
                        }
                    }
                }
                else
                {
                    track.Track.Artists.Clear();
                }
            }
        }

        public event EventHandler<TrackAddingEventArgs> ItemAdding;
        private void OnItemAdding(ImportTrackItem item)
        {
            if (this.ItemAdding != null)
            {
                this.ItemAdding(this, new TrackAddingEventArgs(item));
            }
        }
    }
}
