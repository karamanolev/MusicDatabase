using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.ImportExport
{
    public abstract class CollectionImporterBase : IDisposable
    {
        protected CollectionManager collectionManager;
        protected byte[] readBuffer;
        protected Action<Release, ICollectionImageHandler> updateThumbnailAction;

        public CollectionImporterBase(CollectionManager collectionManager, Action<Release, ICollectionImageHandler> updateThumbnailAction)
        {
            this.collectionManager = collectionManager;
            this.readBuffer = new byte[128 * 1024];
            this.updateThumbnailAction = updateThumbnailAction;
        }

        public virtual void Dispose()
        {
        }

        public virtual IEnumerable<Stream> GetReleaseStreams()
        {
            yield break;
        }

        protected XmlReader CreateReader(Stream stream)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings()
            {
                ConformanceLevel = ConformanceLevel.Document,
                IgnoreComments = true,
                IgnoreWhitespace = true
            };
            return XmlReader.Create(stream, readerSettings);
        }

        public Release ImportRelease(Stream stream)
        {
            using (XmlReader reader = this.CreateReader(stream))
            {
                reader.Read();
                Assert.IsTrue(reader.NodeType == XmlNodeType.XmlDeclaration);

                reader.Read();
                reader.AssertElementStart("release");

                Release release = this.ReadRelease(reader);
                this.updateThumbnailAction(release, this.collectionManager.ImageHandler);
                release.UpdateDynamicProperties();

                return release;
            }
        }

        public void Import()
        {
            using (var transaction = this.collectionManager.BeginTransaction())
            {
                foreach (Stream stream in this.GetReleaseStreams())
                {
                    this.collectionManager.SaveOrUpdate(this.ImportRelease(stream));
                }

                transaction.Commit();
            }
        }

        protected Release ReadRelease(XmlReader reader)
        {
            reader.AssertElementStart("release");

            Release release = new Release();
            release.JoinedAlbumArtists = reader.GetAttributeOrNull("joinedAlbumArtists");
            release.Title = reader.GetAttributeOrNull("title");
            release.ReleaseDate = ReleaseDate.Parse(reader.GetAttributeOrNull("releaseDate"));
            release.OriginalReleaseDate = ReleaseDate.Parse(reader.GetAttributeOrNull("originalReleaseDate"));
            release.CatalogNumber = reader.GetAttributeOrNull("catalogNumber");
            release.Label = reader.GetAttributeOrNull("label");
            release.Country = reader.GetAttributeOrNull("country");
            release.DiscCount = int.Parse(reader.GetAttribute("discCount"));
            release.FlagMessage = reader.GetAttributeOrNull("flagMessage");
            release.IsFlagged = release.FlagMessage != null;
            release.Notes = reader.GetAttributeOrNull("notes");
            release.WikipediaPageName = reader.GetAttributeOrNull("wikipediaPageName");
            release.DiscogsReleaseId = reader.GetAttribute("discogsReleaseId") != null ? int.Parse(reader.GetAttribute("discogsReleaseId")) : 0;
            release.DiscogsMasterId = reader.GetAttribute("discogsMasterId") != null ? int.Parse(reader.GetAttribute("discogsMasterId")) : 0;
            release.Genre = reader.GetAttributeOrNull("genre");
            release.Score = reader.GetAttribute("score") != null ? int.Parse(reader.GetAttribute("score")) : 0;
            release.DynamicRange = reader.GetAttribute("dynamicRange") != null ? int.Parse(reader.GetAttribute("dynamicRange")) : double.NaN;
            release.ReplayGainAlbumGain = reader.GetAttribute("albumGain") != null ? int.Parse(reader.GetAttribute("albumGain")) : double.NaN;
            release.ReplayGainAlbumPeak = reader.GetAttribute("albumPeak") != null ? int.Parse(reader.GetAttribute("albumPeak")) : double.NaN;

            if (reader.GetAttributeOrNull("dateAdded") != null)
            {
                release.DateAdded = new DateTime(long.Parse(reader.GetAttribute("dateAdded")));
            }
            if (reader.GetAttributeOrNull("dateAudioModified") != null)
            {
                release.DateAudioModified = new DateTime(long.Parse(reader.GetAttribute("dateAudioModified")));
            }
            if (reader.GetAttributeOrNull("dateModified") != null)
            {
                release.DateModified = new DateTime(long.Parse(reader.GetAttribute("dateModified")));
            }

            if (reader.IsEmptyElement)
            {
                throw new FormatException("Invalid XML database.");
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd("release"))
                {
                    break;
                }

                if (reader.IsElementStart("artists"))
                {
                    this.ReadArtists(reader, release);
                }
                else if (reader.IsElementStart("tracks"))
                {
                    this.ReadTracks(reader, release);
                }
                else if (reader.IsElementStart("images"))
                {
                    this.ReadImages(reader, release);
                }
                else if (reader.IsElementStart("additionalFiles"))
                {
                    this.ReadAdditionalFiles(reader, release);
                }
            }

            return release;
        }

        private void ReadTracks(XmlReader reader, Release release)
        {
            reader.AssertElementStart("tracks");

            if (reader.IsEmptyElement)
            {
                throw new FormatException("Invalid XML database.");
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd("tracks"))
                {
                    break;
                }

                if (reader.IsElementStart("track"))
                {
                    Track track = this.ReadTrack(reader);
                    release.Tracklist.Add(track);
                }
                else
                {
                    throw new FormatException("Invalid XML database.");
                }
            }
        }

        private Track ReadTrack(XmlReader reader)
        {
            reader.AssertElementStart("track");

            Track track = new Track()
            {
                Disc = int.Parse(reader.GetAttributeOrNull("disc")),
                Position = int.Parse(reader.GetAttributeOrNull("position")),
                Title = reader.GetAttributeOrNull("title"),
                JoinedArtists = reader.GetAttributeOrNull("joinedArtists"),
                RelativeFilename = reader.GetAttributeOrNull("relativeFilename"),
                DynamicRange = reader.GetAttributeOrNull("dynamicRange") != null ? double.Parse(reader.GetAttributeOrNull("dynamicRange")) : double.NaN,
                ReplayGainTrackGain = reader.GetAttributeOrNull("trackGain") != null ? double.Parse(reader.GetAttributeOrNull("trackGain")) : double.NaN,
                ReplayGainTrackPeak = reader.GetAttributeOrNull("trackPeak") != null ? double.Parse(reader.GetAttributeOrNull("trackPeak")) : double.NaN,
            };

            if (reader.IsEmptyElement)
            {
                return track;
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd("track"))
                {
                    break;
                }

                if (reader.IsElementStart("artists"))
                {
                    this.ReadTrackArtists(reader, track);
                }
                else
                {
                    throw new FormatException("Invalid XML database.");
                }
            }

            return track;
        }

        private void ReadTrackArtists(XmlReader reader, Track track)
        {
            reader.AssertElementStart("artists");

            if (reader.IsEmptyElement)
            {
                throw new FormatException("Invalid XML database.");
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd("artists"))
                {
                    break;
                }

                reader.AssertElementStart("artist");
                track.Artists.Add(new TrackArtist()
                {
                    Artist = this.collectionManager.GetOrCreateArtist(reader.GetAttributeOrNull("name")),
                    JoinString = reader.GetAttributeOrNull("join")
                });
            }
        }

        private void ReadAdditionalFiles(XmlReader reader, Release release)
        {
            reader.AssertElementStart("additionalFiles");

            if (reader.IsEmptyElement)
            {
                return;
            }

            bool skipRead = false;
            while (skipRead || reader.Read())
            {
                skipRead = false;

                if (reader.IsElementEnd("additionalFiles"))
                {
                    break;
                }

                reader.AssertElementStart("additionalFile");
                ReleaseAdditionalFile file = new ReleaseAdditionalFile()
                {
                    Type = Utility.ParseEnum<ReleaseAdditionalFileType>(reader.GetAttributeOrNull("type")),
                    Description = reader.GetAttributeOrNull("description"),
                    OriginalFilename = reader.GetAttributeOrNull("originalFilename")
                };

                byte[] resultBuffer = new byte[0];
                int read;
                while ((read = reader.ReadElementContentAsBase64(this.readBuffer, 0, this.readBuffer.Length)) > 0)
                {
                    byte[] newResultBuffer = new byte[resultBuffer.Length + read];
                    Array.Copy(resultBuffer, newResultBuffer, resultBuffer.Length);
                    Array.Copy(this.readBuffer, 0, newResultBuffer, resultBuffer.Length, read);
                    resultBuffer = newResultBuffer;
                }
                file.File = resultBuffer;

                release.AdditionalFiles.Add(file);

                skipRead = true;
            }
        }

        private void ReadImages(XmlReader reader, Release release)
        {
            reader.AssertElementStart("images");

            if (reader.IsEmptyElement)
            {
                return;
            }

            bool skipRead = false;
            while (skipRead || reader.Read())
            {
                skipRead = false;

                if (reader.IsElementEnd("images"))
                {
                    break;
                }

                reader.AssertElementStart("image");
                Image image = new Image()
                {
                    Type = Utility.ParseEnum<ImageType>(reader.GetAttributeOrNull("type")),
                    MimeType = reader.GetAttributeOrNull("mimeType"),
                    Extension = reader.GetAttributeOrNull("extension"),
                    Description = reader.GetAttributeOrNull("description"),
                    IsMain = bool.Parse(reader.GetAttributeOrNull("isMain"))
                };
                release.Images.Add(image);

                release.UpdateDynamicProperties();
                this.collectionManager.SaveOrUpdate(image);

                this.collectionManager.ImageHandler.StoreImageFromXml(image, reader);

                skipRead = true;
            }
        }

        private void ReadArtists(XmlReader reader, Release release)
        {
            reader.AssertElementStart("artists");

            if (reader.IsEmptyElement)
            {
                throw new FormatException("Invalid XML database.");
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd("artists"))
                {
                    break;
                }

                reader.AssertElementStart("artist");
                release.Artists.Add(new ReleaseArtist()
                {
                    Artist = this.collectionManager.GetOrCreateArtist(reader.GetAttributeOrNull("name")),
                    JoinString = reader.GetAttributeOrNull("join")
                });
            }
        }
    }
}
