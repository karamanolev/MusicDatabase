using System;
using System.Collections.Generic;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public class CollectionStatisticsGenerator
    {
        private CollectionManager collectionManager;

        public CollectionStatisticsGenerator(CollectionManager collectionManager)
        {
            this.collectionManager = collectionManager;
        }

        public CollectionStatistics ComputeStatistics()
        {
            CollectionStatistics statistics = new CollectionStatistics();

            HashSet<string> albumArtists = new HashSet<string>();
            HashSet<Artist> artists = new HashSet<Artist>();

            Release[] releases = this.collectionManager.Releases.ToArray();

            int processedReleases = 0;
            foreach (Release release in releases)
            {
                statistics.TotalReleases += 1;
                statistics.TotalAlbumArtists += albumArtists.Add(release.JoinedAlbumArtists) ? 1 : 0;
                statistics.TotalArtists += release.Artists.Select(a => artists.Add(a.Artist) ? 1 : 0).Sum();
                statistics.TotalArtists += release.Tracklist.Select(t => t.Artists.Select(a => artists.Add(a.Artist) ? 1 : 0).Sum()).Sum();
                statistics.TotalTracks += release.Tracklist.Count;

                bool isPerfect = this.IsReleasePerfect(release);

                if (isPerfect)
                {
                    statistics.PerfectReleases += 1;
                }

                if (release.Images.Count > 0)
                {
                    statistics.ReleasesWithImages += 1;
                }

                statistics.TotalImages += release.Images.Count;
                statistics.TotalImageBytes += release.Images.Select(i => this.collectionManager.ImageHandler.GetImageByteLength(i)).Sum();
                statistics.TotalAdditionalFiles += release.AdditionalFiles.Count;
                statistics.TotalAdditionalFileBytes += release.AdditionalFiles.Select(f => f.File.Length).Sum();
                statistics.FlaggedReleases += release.IsFlagged ? 1 : 0;

                ++processedReleases;
                ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs((double)processedReleases / releases.Length);
                this.OnProgressChanged(eventArgs);
                if (eventArgs.Cancel)
                {
                    return null;
                }
            }

            return statistics;
        }

        public bool IsReleasePerfect(Release release)
        {
            if (release.Images.Count == 0)
            {
                return false;
            }

            if (release.AdditionalFiles.Count == 0)
            {
                return false;
            }

            if (string.IsNullOrEmpty(release.Label))
            {
                return false;
            }

            if (string.IsNullOrEmpty(release.CatalogNumber))
            {
                return false;
            }

            if (release.IsFlagged)
            {
                return false;
            }

            if (release.ReleaseDate.Type == ReleaseDateType.Invalid)
            {
                return false;
            }

            if (string.IsNullOrEmpty(release.Country))
            {
                return false;
            }

            if (release.DiscogsReleaseId == 0)
            {
                return false;
            }

            if (release.DiscogsMasterId == 0)
            {
                return false;
            }

            return true;
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
