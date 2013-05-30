using System;
using System.IO;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Local
{
    public class LocalCollectionMatcher
    {
        private LocalCollectionScanner collectionScanner;
        private CollectionManager collectionManager;

        public LocalCollectionMatcher(CollectionManager collectionManager)
        {
            this.collectionScanner = new LocalCollectionScanner(collectionManager);
            this.collectionScanner.ProgressChanged += (sender, e) =>
            {
                this.OnCollectionScannerProgressChanged(e);
            };

            this.collectionManager = collectionManager;
        }

        public LocalCollectionMatcherResult Match()
        {
            LocalCollectionMatcherResult result = new LocalCollectionMatcherResult();

            foreach (Release release in this.collectionManager.Releases)
            {
                result.DeletedReleases.Add(release);
            }

            this.collectionScanner.Scan();

            int totalAlbums = this.collectionScanner.LocalCollection.AlbumArtists.Values.Select(r => r.Albums.Count).Sum();
            int scannedAlbums = 0;

            LocalCollection collection = this.collectionScanner.LocalCollection;
            foreach (LocalAlbumArtist lAlbumArtist in collection.AlbumArtists.Values)
            {
                foreach (LocalAlbum lAlbum in lAlbumArtist.Albums.Values)
                {
                    Release release = this.collectionManager.Operations.GetReleaseByLocalAlbumName(lAlbum);

                    if (release == null)
                    {
                        result.NewReleases.Add(lAlbum);
                    }
                    else
                    {
                        result.DeletedReleases.Remove(release);

                        bool filesExist = release.Tracklist.All(t => {
                            string musicDirectory = this.collectionManager.Settings.MusicDirectory;
                            return File.Exists(Path.Combine(musicDirectory, t.RelativeFilename));
                        });

                        if (!filesExist && !this.collectionManager.Operations.MatchReleaseWithLocalAlbum(release, lAlbum))
                        {
                            result.ChangedReleases.Add(new Tuple<Release, LocalAlbum>(release, lAlbum));
                        }
                        else
                        {
                            result.UnchangedReleases.Add(release);
                        }
                    }

                    ++scannedAlbums;
                    ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs((double)scannedAlbums / totalAlbums);
                    this.OnCollectionMatcherProgressChanged(eventArgs);
                    if (eventArgs.Cancel)
                    {
                        return result;
                    }
                }
            }

            return result;
        }

        public event EventHandler<ProgressChangedEventArgs> CollectionScannerProgressChanged;
        private void OnCollectionScannerProgressChanged(ProgressChangedEventArgs eventArgs)
        {
            if (this.CollectionScannerProgressChanged != null)
            {
                this.CollectionScannerProgressChanged(this, eventArgs);
            }
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        private void OnCollectionMatcherProgressChanged(ProgressChangedEventArgs eventArgs)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, eventArgs);
            }
        }
    }
}
