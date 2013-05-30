using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Local
{
    public class LocalCollectionScanner
    {
        private CollectionManager collectionManager;

        public LocalCollection LocalCollection { get; private set; }

        public LocalCollectionScanner(CollectionManager collectionManager)
        {
            this.collectionManager = collectionManager;
            this.LocalCollection = new LocalCollection();
        }

        public void Scan()
        {
            Dictionary<string, TrackInfoCache> cache = new Dictionary<string, TrackInfoCache>();

            foreach (TrackInfoCache localTrackInfo in this.collectionManager.LocalTrackInfos)
            {
                cache[localTrackInfo.Filename] = localTrackInfo;
            }

            using (var transaction = this.collectionManager.BeginTransaction())
            {
                string[] files;
                if (Directory.Exists(this.collectionManager.Settings.MusicDirectory))
                {
                    files = Directory.GetFiles(this.collectionManager.Settings.MusicDirectory, "*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".flac") || f.EndsWith(".mp3"))
                    .ToArray();
                }
                else
                {
                    files = new string[0];
                }

                int processed = 0;
                foreach (string file in files)
                {
                    TrackInfoCache trackInfo;
                    FileInfo fileInfo = new FileInfo(file);

                    if (!cache.TryGetValue(file, out trackInfo) || trackInfo.LastWriteTime != fileInfo.LastWriteTime.Ticks)
                    {
                        if (trackInfo == null)
                        {
                            trackInfo = new TrackInfoCache();
                        }

                        trackInfo.Filename = file;
                        trackInfo.RelativeFilename = trackInfo.Filename.Substring(this.collectionManager.Settings.MusicDirectory.Length).Trim('\\').Trim('/');
                        trackInfo.LastWriteTime = fileInfo.LastWriteTime.Ticks;

                        using (TagLib.File mediaFile = TagLib.File.Create(file))
                        {
                            trackInfo.Artist = mediaFile.Tag.JoinedPerformers;
                            trackInfo.AlbumArtist = mediaFile.Tag.JoinedAlbumArtists;
                            trackInfo.Album = mediaFile.Tag.Album;
                            trackInfo.Disc = (int)mediaFile.Tag.Disc;
                            trackInfo.DiscCount = (int)mediaFile.Tag.DiscCount;
                            trackInfo.Track = (int)mediaFile.Tag.Track;
                            trackInfo.TrackCount = (int)mediaFile.Tag.TrackCount;
                            trackInfo.Title = mediaFile.Tag.Title;
                            trackInfo.Genre = mediaFile.Tag.JoinedGenres;
                            trackInfo.Year = (int)mediaFile.Tag.Year;
                        }

                        this.collectionManager.SaveOrUpdate(trackInfo);
                    }

                    this.LocalCollection.Add(trackInfo);

                    ++processed;

                    ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs((double)processed / files.Length);
                    this.OnProgressChanged(eventArgs);
                    if (eventArgs.Cancel)
                    {
                        return;
                    }
                }

                transaction.Commit();
            }
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        private void OnProgressChanged(ProgressChangedEventArgs eventArgs)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, eventArgs);
            }
        }
    }
}
