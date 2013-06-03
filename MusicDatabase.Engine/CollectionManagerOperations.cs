using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Local;
using System.IO;
using MusicDatabase.Engine.Tagging;

namespace MusicDatabase.Engine
{
    public class CollectionManagerOperations
    {
        private ICollectionManager collectionManager;

        public CollectionManagerOperations(ICollectionManager collectionManager)
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

        public List<Track> GenerateTracklistForLocalAlbum(ICollectionManager collectionManager, LocalAlbum album, Release release)
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

            //using (ITransaction transaction = this.collectionManager.BeginTransaction())
            //{
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

                this.collectionManager.Save(release);
            }
            //transaction.Commit();
            //}

            CollectionManagerGlobal.OnCollectionChanged();

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
            //using (var transaction = this.collectionManager.BeginTransaction())
            //{
            foreach (Release release in this.collectionManager.Releases)
            {
                release.UpdateDynamicProperties();
                this.collectionManager.Save(release);

                ++processed;
                progress.Report((double)processed / count);
            }
            //transaction.Commit();
            //}
        }

        public void UpdateReleasesThumbnails(IProgress<double> progress)
        {
            int count = this.collectionManager.Releases.Count();
            int processed = 0;
            //using (var transaction = this.collectionManager.BeginTransaction())
            //{
            foreach (Release release in this.collectionManager.Releases)
            {
                ThumbnailGenerator.UpdateReleaseThumbnail(release, this.collectionManager.ImageHandler);
                this.collectionManager.Save(release);

                ++processed;
                progress.Report((double)processed / count);
            }
            //transaction.Commit();
            //}
        }


        public void MoveTracks(Release release, Func<string, Exception, bool> exceptionHandler)
        {
            string musicDirectory = this.collectionManager.Settings.MusicDirectory;
            string namingPattern = this.collectionManager.Settings.FileNamingPattern;

            foreach (Track track in release.Tracklist)
            {
                if (track.RelativeFilename.EndsWith(".mp3"))
                {
                    continue;
                }

                if (!track.RelativeFilename.EndsWith(".flac"))
                {
                    throw new Exception("Track must end with .flac");
                }

                string oldFilename = Path.Combine(musicDirectory, track.RelativeFilename);
                string newRelativeName = FilenameGenerator.PatternToFilename(
                    namingPattern, release, track) + ".flac";
                string newFilename = Path.Combine(musicDirectory, newRelativeName);

                if (oldFilename == newFilename)
                {
                    continue;
                }

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
                        if (exceptionHandler(oldFilename, moveException))
                        {
                            break;
                        }
                    }
                }

                track.RelativeFilename = newRelativeName;
            }

            this.collectionManager.Save(release);
        }

        public void WriteTags(Release release, IProgress<double> progress = null)
        {
            string musicDirectory = this.collectionManager.Settings.MusicDirectory;

            int tracksFinished = 0;
            foreach (Track track in release.Tracklist)
            {
                AudioFileTag tag = new AudioFileTag(release, track);
                string filename = Path.Combine(musicDirectory, track.RelativeFilename);
                tag.WriteToFile(filename);

                File.SetLastWriteTime(filename, release.DateModified);

                ++tracksFinished;

                if (progress != null)
                {
                    progress.Report((double)tracksFinished / release.Tracklist.Count);
                }
            }
        }
    }
}
