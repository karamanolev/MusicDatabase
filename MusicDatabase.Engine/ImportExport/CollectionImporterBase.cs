using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.ImportExport
{
    public abstract class CollectionImporterBase : IDisposable
    {
        private const string InvalidXmlDatabase = "Invalid XML database.";

        protected ICollectionManager collectionManager;
        protected byte[] readBuffer;

        public CollectionImporterBase(ICollectionManager collectionManager)
        {
            this.collectionManager = collectionManager;
            this.readBuffer = new byte[128 * 1024];
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
                reader.AssertElementStart(Keys.Release);

                Release release = this.ReadRelease(reader);
                ThumbnailGenerator.UpdateReleaseThumbnail(release, this.collectionManager.ImageHandler);
                release.UpdateDynamicProperties();

                return release;
            }
        }

        private void ImportInternal()
        {
            foreach (Stream stream in this.GetReleaseStreams())
            {
                this.collectionManager.Save(this.ImportRelease(stream));
            }
        }

        public void Import()
        {
            if (this.collectionManager is ITransactionalCollectionManager)
            {
                ITransactionalCollectionManager transactional = (ITransactionalCollectionManager)this.collectionManager;
                using (var transaction = transactional.BeginTransaction())
                {
                    this.ImportInternal();
                }
            }
            else
            {
                this.ImportInternal();
            }
        }

        protected Release ReadRelease(XmlReader reader)
        {
            reader.AssertElementStart(Keys.Release);

            Release release = new Release();
            release.JoinedAlbumArtists = reader.GetAttributeOrNull(Keys.JoinedAlbumArtists);
            release.Title = reader.GetAttributeOrNull(Keys.Title);
            release.ReleaseDate = ReleaseDate.Parse(reader.GetAttributeOrNull(Keys.ReleaseDate));
            release.OriginalReleaseDate = ReleaseDate.Parse(reader.GetAttributeOrNull(Keys.OriginalReleaseDate));
            release.CatalogNumber = reader.GetAttributeOrNull(Keys.CatalogNumber);
            release.Label = reader.GetAttributeOrNull(Keys.Label);
            release.Country = reader.GetAttributeOrNull(Keys.Country);
            release.FlagMessage = reader.GetAttributeOrNull(Keys.FlagMessage);
            release.IsFlagged = release.FlagMessage != null;
            release.Notes = reader.GetAttributeOrNull(Keys.Notes);
            release.WikipediaPageName = reader.GetAttributeOrNull(Keys.WikipediaPageName);

            release.DiscogsReleaseId = reader.GetAttributeInt32(Keys.DiscogsReleaseId, 0);
            release.DiscogsMasterId = reader.GetAttributeInt32(Keys.DiscogsMasterId, 0);
            release.Genre = reader.GetAttributeOrNull(Keys.Genre);
            release.Score = reader.GetAttributeInt32(Keys.Score, 0);
            release.DynamicRange = reader.GetAttributeDouble(Keys.DynamicRange, double.NaN);
            release.ReplayGainAlbumGain = reader.GetAttributeDouble(Keys.AlbumGain, double.NaN);
            release.ReplayGainAlbumPeak = reader.GetAttributeDouble(Keys.AlbumPeak, double.NaN);

            if (reader.GetAttributeOrNull(Keys.DateAdded) != null)
            {
                release.DateAdded = new DateTime(reader.GetAttributeInt64(Keys.DateAdded, 0));
            }
            if (reader.GetAttributeOrNull(Keys.DateAudioModified) != null)
            {
                release.DateAudioModified = new DateTime(reader.GetAttributeInt64(Keys.DateAudioModified, 0));
            }
            if (reader.GetAttributeOrNull(Keys.DateModified) != null)
            {
                release.DateModified = new DateTime(reader.GetAttributeInt64(Keys.DateModified, 0));
            }

            if (reader.IsEmptyElement)
            {
                throw new FormatException(InvalidXmlDatabase);
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd(Keys.Release))
                {
                    break;
                }

                if (reader.IsElementStart(Keys.Artists))
                {
                    this.ReadArtists(reader, release);
                }
                else if (reader.IsElementStart(Keys.Tracks))
                {
                    this.ReadTracks(reader, release);
                }
                else if (reader.IsElementStart(Keys.Images))
                {
                    this.ReadImages(reader, release);
                }
                else if (reader.IsElementStart(Keys.AdditionalFiles))
                {
                    this.ReadAdditionalFiles(reader, release);
                }
            }

            return release;
        }

        private void ReadTracks(XmlReader reader, Release release)
        {
            reader.AssertElementStart(Keys.Tracks);

            if (reader.IsEmptyElement)
            {
                throw new FormatException(InvalidXmlDatabase);
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd(Keys.Tracks))
                {
                    break;
                }

                if (reader.IsElementStart(Keys.Track))
                {
                    Track track = this.ReadTrack(reader);
                    release.Tracklist.Add(track);
                }
                else
                {
                    throw new FormatException(InvalidXmlDatabase);
                }
            }
        }

        private Track ReadTrack(XmlReader reader)
        {
            reader.AssertElementStart(Keys.Track);

            Track track = new Track()
            {
                Disc = reader.GetAttributeInt32(Keys.Disc, 0),
                Position = reader.GetAttributeInt32(Keys.Position, 0),
                Title = reader.GetAttributeOrNull(Keys.Title),
                JoinedArtists = reader.GetAttributeOrNull(Keys.JoinedArtists),
                RelativeFilename = reader.GetAttributeOrNull(Keys.RelativeFilename),
                DynamicRange = reader.GetAttributeDouble(Keys.DynamicRange, double.NaN),
                ReplayGainTrackGain = reader.GetAttributeDouble(Keys.TrackGain, double.NaN),
                ReplayGainTrackPeak = reader.GetAttributeDouble(Keys.TrackPeak, double.NaN),
            };

            if (reader.IsEmptyElement)
            {
                return track;
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd(Keys.Track))
                {
                    break;
                }

                if (reader.IsElementStart(Keys.Artists))
                {
                    this.ReadTrackArtists(reader, track);
                }
                else
                {
                    throw new FormatException(InvalidXmlDatabase);
                }
            }

            return track;
        }

        private void ReadTrackArtists(XmlReader reader, Track track)
        {
            reader.AssertElementStart(Keys.Artists);

            if (reader.IsEmptyElement)
            {
                throw new FormatException(InvalidXmlDatabase);
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd(Keys.Artists))
                {
                    break;
                }

                reader.AssertElementStart(Keys.Artist);
                track.Artists.Add(new TrackArtist()
                {
                    Artist = this.collectionManager.GetOrCreateArtist(reader.GetAttributeOrNull(Keys.Name)),
                    JoinString = reader.GetAttributeOrNull(Keys.JoinString)
                });
            }
        }

        private void ReadAdditionalFiles(XmlReader reader, Release release)
        {
            reader.AssertElementStart(Keys.AdditionalFiles);

            if (reader.IsEmptyElement)
            {
                return;
            }

            bool skipRead = false;
            while (skipRead || reader.Read())
            {
                skipRead = false;

                if (reader.IsElementEnd(Keys.AdditionalFiles))
                {
                    break;
                }

                reader.AssertElementStart(Keys.AdditionalFile);
                ReleaseAdditionalFile file = new ReleaseAdditionalFile()
                {
                    Type = Utility.ParseEnum<ReleaseAdditionalFileType>(reader.GetAttributeOrNull(Keys.Type)),
                    Description = reader.GetAttributeOrNull(Keys.Description),
                    OriginalFilename = reader.GetAttributeOrNull(Keys.OriginalFilename)
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
            reader.AssertElementStart(Keys.Images);

            if (reader.IsEmptyElement)
            {
                return;
            }

            bool skipRead = false;
            while (skipRead || reader.Read())
            {
                skipRead = false;

                if (reader.IsElementEnd(Keys.Images))
                {
                    break;
                }

                reader.AssertElementStart(Keys.Image);
                Image image = new Image()
                {
                    Type = Utility.ParseEnum<ImageType>(reader.GetAttributeOrNull(Keys.Type)),
                    MimeType = reader.GetAttributeOrNull(Keys.MimeType),
                    Extension = reader.GetAttributeOrNull(Keys.Extension),
                    Description = reader.GetAttributeOrNull(Keys.Description),
                    IsMain = bool.Parse(reader.GetAttributeOrNull(Keys.IsMain))
                };
                release.Images.Add(image);

                release.UpdateDynamicProperties();

                this.collectionManager.ImageHandler.StoreImageFromXml(image, reader);

                skipRead = true;
            }
        }

        private void ReadArtists(XmlReader reader, Release release)
        {
            reader.AssertElementStart(Keys.Artists);

            if (reader.IsEmptyElement)
            {
                throw new FormatException(InvalidXmlDatabase);
            }

            while (reader.Read())
            {
                if (reader.IsElementEnd(Keys.Artists))
                {
                    break;
                }

                reader.AssertElementStart(Keys.Artist);
                release.Artists.Add(new ReleaseArtist()
                {
                    Artist = this.collectionManager.GetOrCreateArtist(reader.GetAttributeOrNull(Keys.Name)),
                    JoinString = reader.GetAttributeOrNull(Keys.JoinString)
                });
            }
        }
    }
}
