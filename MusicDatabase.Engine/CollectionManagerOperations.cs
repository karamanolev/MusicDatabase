using System.Collections.Generic;
using System.Linq;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Local;
using NHibernate;
using System;
using System.Text;

namespace MusicDatabase.Engine
{
    public class CollectionManagerOperations
    {
        private CollectionManager collectionManager;

        public CollectionManagerOperations(CollectionManager collectionManager)
        {
            this.collectionManager = collectionManager;
        }

        public Release GetReleaseByLocalAlbumName(LocalAlbum localAlbum)
        {
            return this.collectionManager.Releases
                .Where(r => r.Title == localAlbum.Title && r.JoinedAlbumArtists == localAlbum.AlbumArtist)
                .FirstOrDefault();
        }

        public bool MatchReleaseWithLocalAlbum(Release release, LocalAlbum localAlbum)
        {
            if (release.JoinedAlbumArtists != localAlbum.AlbumArtist)
            {
                return false;
            }
            if (release.Title != localAlbum.Title)
            {
                return false;
            }

            if (localAlbum.Year != 0 && release.ReleaseDate.Date.Year != localAlbum.Year)
            {
                return false;
            }
            if (localAlbum.Year == 0 && release.ReleaseDate.IsValid)
            {
                return false;
            }

            if (release.Genre != localAlbum.Genre)
            {
                return false;
            }
            if (release.DiscCount != localAlbum.DiscCount)
            {
                return false;
            }

            if (!MatchReleaseTracklistWithLocalAlbum(release, localAlbum))
            {
                return false;
            }

            return true;
        }

        public bool MatchReleaseTracklistWithLocalAlbum(Release release, LocalAlbum localAlbum)
        {
            List<Track> tracks = this.GenerateTracklistForLocalAlbum(this.collectionManager, localAlbum, null);

            for (int i = 0; i < tracks.Count; ++i)
            {
                Track track = release.Tracklist[i];
                Track localTrack = tracks[i];

                if (track.RelativeFilename != localTrack.RelativeFilename)
                {
                    return false;
                }
                if (track.Title != localTrack.Title)
                {
                    return false;
                }
                if (track.JoinedArtists != localTrack.JoinedArtists)
                {
                    return false;
                }
                if (track.Disc != localTrack.Disc)
                {
                    return false;
                }
                if (track.Position != localTrack.Position)
                {
                    return false;
                }
            }

            foreach (LocalDisc disc in localAlbum.Discs)
            {
                foreach (TrackInfoCache track in disc.Tracks)
                {
                    if (track.Year != 0 && track.Year != release.ReleaseDate.Date.Year)
                    {
                        return false;
                    }
                    if (track.Year == 0 && release.ReleaseDate.IsValid)
                    {
                        return false;
                    }
                    if (track.RelativeFilename != track.RelativeFilename)
                    {
                        return false;
                    }

                    if (track.Genre != release.Genre)
                    {
                        return false;
                    }
                    if (track.DiscCount != release.DiscCount)
                    {
                        return false;
                    }
                    if (track.AlbumArtist != release.JoinedAlbumArtists)
                    {
                        return false;
                    }
                    if (track.Album != release.Title)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public List<Track> GenerateTracklistForLocalAlbum(CollectionManager collectionManager, LocalAlbum album, Release release)
        {
            List<Track> list = new List<Track>();
            foreach (LocalDisc disc in album.Discs)
            {
                bool hasTrackArtists = disc.Tracks.Any(t => t.Artist != disc.Tracks[0].Artist);

                foreach (TrackInfoCache localTrack in disc.Tracks)
                {
                    Track track = new Track()
                    {
                        Disc = localTrack.Disc,
                        Position = localTrack.Track,
                        Title = localTrack.Title,
                        RelativeFilename = localTrack.RelativeFilename
                    };
                    if (hasTrackArtists)
                    {
                        track.Artists.Add(new TrackArtist()
                        {
                            Artist = collectionManager.GetOrCreateArtist(localTrack.Artist),
                        });
                        track.JoinedArtists = localTrack.Artist;
                    }

                    list.Add(track);
                }
            }
            return list;
        }

        public void FixMissingFields(out string message)
        {
            int originalReleaseDate = 0;
            int dateAdded = 0;
            int dateModified = 0;
            int dateAudioModified = 0;

            using (ITransaction transaction = this.collectionManager.BeginTransaction())
            {
                foreach (Release release in this.collectionManager.Releases)
                {
                    if (release.OriginalReleaseDate == null)
                    {
                        release.OriginalReleaseDate = new ReleaseDate();
                        ++originalReleaseDate;
                    }

                    if (release.DateAdded == DateTime.MinValue)
                    {
                        release.DateAdded = DateTime.Now;
                        ++dateAdded;
                    }

                    if (release.DateModified == DateTime.MinValue)
                    {
                        release.DateModified = DateTime.Now;
                        ++dateModified;
                    }

                    if (release.DateAudioModified == DateTime.MinValue)
                    {
                        release.DateAudioModified = release.DateModified;
                        ++dateAudioModified;
                    }
                }

                transaction.Commit();
            }

            CollectionManager.OnCollectionChanged();

            StringBuilder sb = new StringBuilder();
            if (originalReleaseDate != 0)
            {
                sb.AppendLine(originalReleaseDate + " original release dates fixed.");
            }
            if (dateAdded != 0)
            {
                sb.AppendLine(dateAdded + " dates added fixed.");
            }
            if (dateModified != 0)
            {
                sb.AppendLine(dateModified + " dates modified fixed.");
            }
            if (dateAudioModified != 0)
            {
                sb.AppendLine(dateAudioModified + " dates audio modified fixed.");
            }
            sb.AppendLine("Fix complete.");
            message = sb.ToString();
        }

        public void UpdateReleasesDynamicProperties(IProgress<double> progress)
        {
            int count = this.collectionManager.Releases.Count();
            int processed = 0;
            using (var transaction = this.collectionManager.BeginTransaction())
            {
                foreach (Release release in this.collectionManager.Releases)
                {
                    release.UpdateDynamicProperties();
                    this.collectionManager.SaveOrUpdate(release);

                    ++processed;
                    progress.Report((double)processed / count);
                }
                transaction.Commit();
            }
        }

        public void UpdateReleasesThumbnails(IProgress<double> progress, Action<Release> updateThumbnailAction)
        {
            int count = this.collectionManager.Releases.Count();
            int processed = 0;
            using (var transaction = this.collectionManager.BeginTransaction())
            {
                foreach (Release release in this.collectionManager.Releases)
                {
                    updateThumbnailAction(release);
                    this.collectionManager.SaveOrUpdate(release);

                    ++processed;
                    progress.Report((double)processed / count);
                }
                transaction.Commit();
            }
        }
    }
}
