using System;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Local
{
    public class LocalCollectionMerger
    {
        private CollectionManager collectionManager;
        private LocalCollectionMatcherResult matcherResult;
        private bool saveToFiles;
        private int totalProcessed = 0, total;

        public LocalCollectionMerger(CollectionManager collectionManager, LocalCollectionMatcherResult matcherResult, bool saveToFiles)
        {
            this.collectionManager = collectionManager;
            this.matcherResult = matcherResult;
            this.saveToFiles = saveToFiles;

            this.total =
                this.matcherResult.NewReleases.Count +
                this.matcherResult.ChangedReleases.Count +
                this.matcherResult.DeletedReleases.Count;
        }

        public void Merge()
        {
            using (var transaction = this.collectionManager.BeginTransaction())
            {
                this.AddReleases();
                if (this.saveToFiles)
                {
                    this.SaveChangesToFiles();
                }
                else
                {
                    this.LoadChangesFromFiles();
                }

                this.DeleteReleases();

                transaction.Commit();
            }
        }

        private void DeleteReleases()
        {
            foreach (Release release in matcherResult.DeletedReleases)
            {
                this.collectionManager.DeleteRelease(release);

                ++this.totalProcessed;
                this.OnProgressChanged();
            }
        }

        private void SaveChangesToFiles()
        {
            throw new NotImplementedException();
        }

        private void LoadChangesFromFiles()
        {
            foreach (Tuple<Release, LocalAlbum> item in matcherResult.ChangedReleases)
            {
                Release release = item.Item1;
                LocalAlbum album = item.Item2;
                release.Genre = album.Genre ?? release.Genre;
                release.DiscCount = album.DiscCount == 0 ? release.DiscCount : album.DiscCount;
                if (album.Year != 0 && release.ReleaseDate.Date.Year != album.Year)
                {
                    release.ReleaseDate = album.Year == 0 ? new ReleaseDate() : new ReleaseDate(album.Year);
                }
                if (!this.collectionManager.Operations.MatchReleaseTracklistWithLocalAlbum(release, album))
                {
                    release.Tracklist.Clear();
                    release.Tracklist.AddRange(this.collectionManager.Operations.GenerateTracklistForLocalAlbum(this.collectionManager, album, release));
                }
                release.DateModified = DateTime.Now;

                release.UpdateDynamicProperties();
                this.collectionManager.SaveOrUpdate(release);

                ++this.totalProcessed;
                this.OnProgressChanged();
            }
        }

        private void AddReleases()
        {
            foreach (LocalAlbum album in matcherResult.NewReleases)
            {
                Release release = new Release();
                release.JoinedAlbumArtists = album.AlbumArtist;
                release.Artists.Add(new ReleaseArtist()
                {
                    Artist = this.collectionManager.GetOrCreateArtist(album.AlbumArtist)
                });
                release.Title = album.Title;
                if (album.Year == 0)
                {
                    release.ReleaseDate = new ReleaseDate();
                }
                else
                {
                    release.ReleaseDate = new ReleaseDate(album.Year);
                }
                release.Genre = album.Genre;
                release.DiscCount = album.DiscCount;
                release.Tracklist.AddRange(this.collectionManager.Operations.GenerateTracklistForLocalAlbum(this.collectionManager, album, release));

                release.DateAdded = DateTime.Now;
                release.DateModified = DateTime.Now;
                release.DateAudioModified = DateTime.Now;

                release.UpdateDynamicProperties();
                this.collectionManager.SaveOrUpdate(release);

                ++this.totalProcessed;
                this.OnProgressChanged();
            }
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        private void OnProgressChanged()
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, new ProgressChangedEventArgs((double)this.totalProcessed / this.total));
            }
        }
    }
}
