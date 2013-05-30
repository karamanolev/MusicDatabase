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
        protected CollectionManager collectionManager;

        public CollectionExporterBase(CollectionManager collectionManager)
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
            writer.WriteStartElement("release");

            writer.WriteAttributeString("joinedAlbumArtists", release.JoinedAlbumArtists);
            writer.WriteAttributeString("title", release.Title);
            writer.WriteAttributeString("releaseDate", release.ReleaseDate.ToString());
            writer.WriteAttributeString("originalReleaseDate", release.OriginalReleaseDate.ToString());
            writer.WriteAttributeString("catalogNumber", release.CatalogNumber);
            writer.WriteAttributeString("label", release.Label);
            writer.WriteAttributeString("country", release.Country);
            writer.WriteAttributeString("discCount", release.DiscCount.ToString());
            if (release.IsFlagged)
            {
                writer.WriteAttributeString("flagMessage", release.FlagMessage);
            }
            writer.WriteAttributeString("notes", release.Notes);
            if (!string.IsNullOrEmpty(release.WikipediaPageName))
            {
                writer.WriteAttributeString("wikipediaPageName", release.WikipediaPageName);
            }
            if (release.DiscogsReleaseId != 0)
            {
                writer.WriteAttributeString("discogsReleaseId", release.DiscogsReleaseId.ToString());
            }
            if (release.DiscogsMasterId != 0)
            {
                writer.WriteAttributeString("discogsMasterId", release.DiscogsMasterId.ToString());
            }
            writer.WriteAttributeString("genre", release.Genre);

            writer.WriteAttributeString("dateAdded", release.DateAdded.Ticks.ToString());
            writer.WriteAttributeString("dateAudioModified", release.DateAudioModified.Ticks.ToString());
            writer.WriteAttributeString("dateModified", release.DateModified.Ticks.ToString());
            writer.WriteAttributeString("score", release.Score.ToString());
            writer.WriteAttributeString("dynamicRange", release.DynamicRange.ToString());
            writer.WriteAttributeString("albumGain", release.ReplayGainAlbumGain.ToString());
            writer.WriteAttributeString("albumPeak", release.ReplayGainAlbumPeak.ToString());

            this.ExportReleaseArtists(writer, release);
            this.ExportReleaseTracklist(writer, release);
            this.ExportReleaseAdditionalFiles(writer, release);
            this.ExportReleaseImages(writer, release);

            writer.WriteEndElement();
        }

        private void ExportReleaseArtists(XmlWriter writer, Release release)
        {
            writer.WriteStartElement("artists");

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
            writer.WriteStartElement("artist");

            writer.WriteAttributeString("name", releaseArtist.Artist.Name);
            if (!string.IsNullOrEmpty(releaseArtist.JoinString))
            {
                writer.WriteAttributeString("join", releaseArtist.JoinString);
            }

            writer.WriteEndElement();
        }

        private void ExportReleaseAdditionalFiles(XmlWriter writer, Release release)
        {
            writer.WriteStartElement("additionalFiles");

            foreach (ReleaseAdditionalFile additionalFile in release.AdditionalFiles)
            {
                this.ExportAdditionalFile(writer, additionalFile);
            }

            writer.WriteEndElement();
        }

        private void ExportAdditionalFile(XmlWriter writer, ReleaseAdditionalFile additionalFile)
        {
            writer.WriteStartElement("additionalFile");

            writer.WriteAttributeString("type", additionalFile.Type.ToString());
            writer.WriteAttributeString("description", additionalFile.Description);
            writer.WriteAttributeString("originalFilename", additionalFile.OriginalFilename);
            writer.WriteBase64(additionalFile.File, 0, additionalFile.File.Length);

            writer.WriteEndElement();
        }

        private void ExportReleaseImages(XmlWriter writer, Release release)
        {
            writer.WriteStartElement("images");

            foreach (Image image in release.Images)
            {
                this.ExportImage(writer, image);
            }

            writer.WriteEndElement();
        }

        private void ExportImage(XmlWriter writer, Image image)
        {
            writer.WriteStartElement("image");

            writer.WriteAttributeString("type", image.Type.ToString());
            if (image.MimeType == "application/unknown")
            {
                image.MimeType = MimeHelper.GetMimeTypeForExtension(image.Extension);
            }
            writer.WriteAttributeString("mimeType", image.MimeType);
            writer.WriteAttributeString("extension", image.Extension);
            writer.WriteAttributeString("description", image.Description);
            writer.WriteAttributeString("isMain", image.IsMain.ToString());

            byte[] imageBytes = this.collectionManager.ImageHandler.LoadImage(image);
            writer.WriteBase64(imageBytes, 0, imageBytes.Length);

            writer.WriteEndElement();
        }

        private void ExportReleaseTracklist(XmlWriter writer, Release release)
        {
            writer.WriteStartElement("tracks");

            foreach (Track track in release.Tracklist)
            {
                this.ExportTrack(writer, track);
            }

            writer.WriteEndElement();
        }

        private void ExportTrack(XmlWriter writer, Track track)
        {
            writer.WriteStartElement("track");

            writer.WriteAttributeString("disc", track.Disc.ToString());
            writer.WriteAttributeString("position", track.Position.ToString());
            writer.WriteAttributeString("title", track.Title);
            if (!string.IsNullOrEmpty(track.JoinedArtists))
            {
                writer.WriteAttributeString("joinedArtists", track.JoinedArtists);
            }
            writer.WriteAttributeString("relativeFilename", track.RelativeFilename);
            writer.WriteAttributeString("dynamicRange", track.DynamicRange.ToString());
            writer.WriteAttributeString("trackGain", track.ReplayGainTrackGain.ToString());
            writer.WriteAttributeString("trackPeak", track.ReplayGainTrackPeak.ToString());

            this.ExportTrackArtists(writer, track);

            writer.WriteEndElement();
        }

        private void ExportTrackArtists(XmlWriter writer, Track track)
        {
            if (!string.IsNullOrEmpty(track.JoinedArtists))
            {
                writer.WriteStartElement("artists");

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
            writer.WriteStartElement("artist");

            writer.WriteAttributeString("name", releaseArtist.Artist.Name);
            if (!string.IsNullOrEmpty(releaseArtist.JoinString))
            {
                writer.WriteAttributeString("join", releaseArtist.JoinString);
            }

            writer.WriteEndElement();
        }
    }
}