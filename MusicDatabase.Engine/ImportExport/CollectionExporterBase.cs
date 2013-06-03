using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.ImportExport
{
    public abstract class CollectionExporterBase : IDisposable
    {
        protected ICollectionManager collectionManager;

        public CollectionExporterBase(ICollectionManager collectionManager)
        {

            this.collectionManager = collectionManager;
        }

        public virtual void Dispose()
        {
        }

        protected abstract IEnumerable<Stream> GetEntryOutputStream(string entryName, DateTime dateModified, object obj);

        public void Export(IProgress<double> progress = null)
        {
            Release[] releases = this.collectionManager.Releases.ToArray();

            double processed = 0;
            double total = releases.Length;

            foreach (Release release in releases)
            {
                string entryName = "Releases\\" + FilenameGenerator.FixFilename(release.JoinedAlbumArtists + " - " + release.Title) + ".xml";

                foreach (Stream stream in this.GetEntryOutputStream(entryName, release.DateModified, release))
                {
                    XmlWriterSettings writerSettings = new XmlWriterSettings()
                    {
                        ConformanceLevel = ConformanceLevel.Document,
                        Indent = true
                    };
                    using (XmlWriter writer = XmlTextWriter.Create(stream, writerSettings))
                    {
                        this.ExportRelease(writer, release);
                    }
                }

                if (progress != null)
                {
                    ++processed;
                    progress.Report(processed / total);
                }
            }
        }


        private void ExportRelease(XmlWriter writer, Release release)
        {
            writer.WriteStartElement(Keys.Release);

            writer.WriteAttributeString(Keys.JoinedAlbumArtists, release.JoinedAlbumArtists);
            writer.WriteAttributeString(Keys.Title, release.Title);
            writer.WriteAttributeString(Keys.ReleaseDate, release.ReleaseDate.ToString());
            writer.WriteAttributeString(Keys.OriginalReleaseDate, release.OriginalReleaseDate.ToString());
            writer.WriteAttributeString(Keys.CatalogNumber, release.CatalogNumber);
            writer.WriteAttributeString(Keys.Label, release.Label);
            writer.WriteAttributeString(Keys.Country, release.Country);
            writer.WriteAttributeString(Keys.Genre, release.Genre);
            writer.WriteAttributeString(Keys.DateAdded, release.DateAdded.Ticks.ToString());
            writer.WriteAttributeString(Keys.DateAudioModified, release.DateAudioModified.Ticks.ToString());
            writer.WriteAttributeString(Keys.DateModified, release.DateModified.Ticks.ToString());
            writer.WriteAttributeString(Keys.Score, release.Score.ToString());
            writer.WriteAttributeString(Keys.DynamicRange, release.DynamicRange.ToString());
            writer.WriteAttributeString(Keys.AlbumGain, release.ReplayGainAlbumGain.ToString());
            writer.WriteAttributeString(Keys.AlbumPeak, release.ReplayGainAlbumPeak.ToString());
            writer.WriteAttributeString(Keys.Notes, release.Notes);
            if (release.IsFlagged)
            {
                writer.WriteAttributeString(Keys.FlagMessage, release.FlagMessage);
            }
            if (!string.IsNullOrEmpty(release.WikipediaPageName))
            {
                writer.WriteAttributeString(Keys.WikipediaPageName, release.WikipediaPageName);
            }
            if (release.DiscogsReleaseId != 0)
            {
                writer.WriteAttributeString(Keys.DiscogsReleaseId, release.DiscogsReleaseId.ToString());
            }
            if (release.DiscogsMasterId != 0)
            {
                writer.WriteAttributeString(Keys.DiscogsMasterId, release.DiscogsMasterId.ToString());
            }

            this.ExportReleaseArtists(writer, release);
            this.ExportReleaseTracklist(writer, release);
            this.ExportReleaseAdditionalFiles(writer, release);
            this.ExportReleaseImages(writer, release);

            writer.WriteEndElement();
        }

        private void ExportReleaseArtists(XmlWriter writer, Release release)
        {
            writer.WriteStartElement(Keys.Artists);

            ReleaseArtist[] artists;
            try
            {
                artists = release.Artists.ToArray();
                release.Artists.Select(a => a.Artist.Name).ToArray(); // get names to verify
            }
            catch
            {
                artists = new ReleaseArtist[]
                {
                    new ReleaseArtist()
                    {
                        Artist = new Artist()
                                 {
                                     Name = release.JoinedAlbumArtists
                                 }
                    }
                };
            }
            foreach (ReleaseArtist releaseArtist in artists)
            {
                this.ExportReleaseArtist(writer, releaseArtist);
            }

            writer.WriteEndElement();
        }

        private void ExportReleaseArtist(XmlWriter writer, ReleaseArtist releaseArtist)
        {
            writer.WriteStartElement(Keys.Artist);

            writer.WriteAttributeString(Keys.Name, releaseArtist.Artist.Name);
            if (!string.IsNullOrEmpty(releaseArtist.JoinString))
            {
                writer.WriteAttributeString(Keys.JoinString, releaseArtist.JoinString);
            }

            writer.WriteEndElement();
        }

        private void ExportReleaseAdditionalFiles(XmlWriter writer, Release release)
        {
            writer.WriteStartElement(Keys.AdditionalFiles);

            foreach (ReleaseAdditionalFile additionalFile in release.AdditionalFiles)
            {
                this.ExportAdditionalFile(writer, additionalFile);
            }

            writer.WriteEndElement();
        }

        private void ExportAdditionalFile(XmlWriter writer, ReleaseAdditionalFile additionalFile)
        {
            writer.WriteStartElement(Keys.AdditionalFile);

            writer.WriteAttributeString(Keys.Type, additionalFile.Type.ToString());
            writer.WriteAttributeString(Keys.Description, additionalFile.Description);
            writer.WriteAttributeString(Keys.OriginalFilename, additionalFile.OriginalFilename);
            writer.WriteBase64(additionalFile.File, 0, additionalFile.File.Length);

            writer.WriteEndElement();
        }

        private void ExportReleaseImages(XmlWriter writer, Release release)
        {
            writer.WriteStartElement(Keys.Images);

            foreach (Image image in release.Images)
            {
                this.ExportImage(writer, image);
            }

            writer.WriteEndElement();
        }

        private void ExportImage(XmlWriter writer, Image image)
        {
            writer.WriteStartElement(Keys.Image);

            writer.WriteAttributeString(Keys.Type, image.Type.ToString());
            if (image.MimeType == "application/unknown")
            {
                image.MimeType = MimeHelper.GetMimeTypeForExtension(image.Extension);
            }
            writer.WriteAttributeString(Keys.MimeType, image.MimeType);
            writer.WriteAttributeString(Keys.Extension, image.Extension);
            writer.WriteAttributeString(Keys.Description, image.Description);
            writer.WriteAttributeString(Keys.IsMain, image.IsMain.ToString());

            byte[] imageBytes = this.collectionManager.ImageHandler.LoadImage(image);
            writer.WriteBase64(imageBytes, 0, imageBytes.Length);

            writer.WriteEndElement();
        }

        private void ExportReleaseTracklist(XmlWriter writer, Release release)
        {
            writer.WriteStartElement(Keys.Tracks);

            foreach (Track track in release.Tracklist)
            {
                this.ExportTrack(writer, track);
            }

            writer.WriteEndElement();
        }

        private void ExportTrack(XmlWriter writer, Track track)
        {
            writer.WriteStartElement(Keys.Track);

            writer.WriteAttributeString(Keys.Disc, track.Disc.ToString());
            writer.WriteAttributeString(Keys.Position, track.Position.ToString());
            writer.WriteAttributeString(Keys.Title, track.Title);
            if (!string.IsNullOrEmpty(track.JoinedArtists))
            {
                writer.WriteAttributeString(Keys.JoinedArtists, track.JoinedArtists);
            }
            writer.WriteAttributeString(Keys.RelativeFilename, track.RelativeFilename);
            writer.WriteAttributeString(Keys.DynamicRange, track.DynamicRange.ToString());
            writer.WriteAttributeString(Keys.TrackGain, track.ReplayGainTrackGain.ToString());
            writer.WriteAttributeString(Keys.TrackPeak, track.ReplayGainTrackPeak.ToString());

            this.ExportTrackArtists(writer, track);

            writer.WriteEndElement();
        }

        private void ExportTrackArtists(XmlWriter writer, Track track)
        {
            if (!string.IsNullOrEmpty(track.JoinedArtists))
            {
                writer.WriteStartElement(Keys.Artists);

                TrackArtist[] artists = null;
                try
                {
                    artists = track.Artists.ToArray();
                    track.Artists.Select(a => a.Artist.Name).ToArray(); // get names to verify
                }
                catch
                {
                    artists = new TrackArtist[]
                    {
                        new TrackArtist()
                        {
                            Artist = new Artist()
                                     {
                                         Name = track.JoinedArtists
                                     }
                        }
                    };
                }

                if (artists != null)
                {
                    foreach (TrackArtist artist in artists)
                    {
                        this.ExportTrackArtist(writer, artist);
                    }
                }

                writer.WriteEndElement();
            }
        }

        private void ExportTrackArtist(XmlWriter writer, TrackArtist releaseArtist)
        {
            writer.WriteStartElement(Keys.Artist);

            writer.WriteAttributeString(Keys.Name, releaseArtist.Artist.Name);
            if (!string.IsNullOrEmpty(releaseArtist.JoinString))
            {
                writer.WriteAttributeString(Keys.JoinString, releaseArtist.JoinString);
            }

            writer.WriteEndElement();
        }
    }
}