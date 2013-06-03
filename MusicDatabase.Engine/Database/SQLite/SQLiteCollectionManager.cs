using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Database.SQLite
{
    public class SQLiteCollectionManager : ITransactionalCollectionManager
    {
        private SQLiteConnection connection;
        private SQLiteCommand loadSettings, saveSettings;
        private SQLiteCommand loadTrackCaches, saveTrackCache;
        private SQLiteCommand loadReleases, loadReleaseById, countReleases;
        private SQLiteCommand saveRelease, updateRelease, deleteRelease;
        private BsonBinaryWriterSettings bsonWriterSettings;
        private BsonBinaryReaderSettings bsonReaderSettings;

        public ICollectionImageHandler ImageHandler { get; private set; }

        private IEnumerable<Release> ReleasesEnumerable
        {
            get
            {
                using (var reader = this.loadReleases.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return this.CreateRelease(reader);
                    }
                }
            }
        }

        public IQueryable<Release> Releases
        {
            get { return this.ReleasesEnumerable.AsQueryable<Release>(); }
        }

        public int ReleaseCount
        {
            get
            {
                using (var reader = this.countReleases.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return reader.GetInt32(0);
                    }
                    return 0;
                }
            }
        }

        private IEnumerable<TrackInfoCache> LocalTrackInfosEnumerable
        {
            get
            {
                using (var reader = this.loadTrackCaches.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return this.CreateTrackInfoCache(reader);
                    }
                }
            }
        }

        public IQueryable<TrackInfoCache> LocalTrackInfos
        {
            get { return this.LocalTrackInfosEnumerable.AsQueryable<TrackInfoCache>(); }
        }

        public CollectionSettings Settings { get; private set; }

        public CollectionManagerOperations Operations { get; private set; }

        public SQLiteCollectionManager(SQLiteConnection connection, ICollectionImageHandler imageHandler)
        {
            this.connection = connection;
            this.ImageHandler = imageHandler;

            this.bsonWriterSettings = new BsonBinaryWriterSettings()
            {
                MaxDocumentSize = 24 * 1024 * 1024
            };
            this.bsonReaderSettings = new BsonBinaryReaderSettings()
            {
                MaxDocumentSize = this.bsonWriterSettings.MaxDocumentSize
            };

            this.Operations = new CollectionManagerOperations(this);

            this.loadSettings = this.connection.CreateCommand();
            this.loadSettings.CommandText = "SELECT * FROM settings;";

            this.saveSettings = this.connection.CreateCommand();
            this.saveSettings.CommandText = "DELETE FROM settings; INSERT INTO settings (bson) VALUES (@bson);";
            this.saveSettings.Parameters.Add("bson", DbType.Binary);

            this.loadTrackCaches = this.connection.CreateCommand();
            this.loadTrackCaches.CommandText = "SELECT * FROM track_info_caches;";

            this.saveTrackCache = this.connection.CreateCommand();
            this.saveTrackCache.CommandText = "INSERT INTO track_info_caches (bson) VALUES (@bson);";
            this.saveTrackCache.Parameters.Add("bson", DbType.Binary);

            this.loadReleases = this.connection.CreateCommand();
            this.loadReleases.CommandText = "SELECT * FROM releases;";

            this.countReleases = this.connection.CreateCommand();
            this.countReleases.CommandText = "SELECT COUNT(*) FROM releases;";

            this.loadReleaseById = this.connection.CreateCommand();
            this.loadReleaseById.CommandText = "SELECT * FROM releases WHERE id = @id;";
            this.loadReleaseById.Parameters.Add("id", DbType.Int64);

            this.saveRelease = this.connection.CreateCommand();
            this.saveRelease.CommandText = "INSERT INTO releases (bson) VALUES (@bson);";
            this.saveRelease.Parameters.Add("bson", DbType.Binary);

            this.updateRelease = this.connection.CreateCommand();
            this.updateRelease.CommandText = "UPDATE releases SET bson = @bson WHERE id = @id;";
            this.updateRelease.Parameters.Add("id", DbType.Int64);
            this.updateRelease.Parameters.Add("bson", DbType.Binary);

            this.deleteRelease = this.connection.CreateCommand();
            this.deleteRelease.CommandText = "DELETE FROM releases WHERE id = @id;";
            this.deleteRelease.Parameters.Add("id", DbType.Int64);

            this.ReloadSettings();
        }

        private T Deserialize<T>(byte[] bson)
        {
            using (MemoryStream stream = new MemoryStream(bson))
            {
                using (BsonReader reader = BsonReader.Create(stream, this.bsonReaderSettings))
                {
                    return BsonSerializer.Deserialize<T>(reader);
                }
            }
        }

        private Release CreateRelease(SQLiteDataReader reader)
        {
            Release release = this.Deserialize<Release>(reader.GetBytes("bson"));
            release.Id = reader.GetInt64("id").ToString();
            return release;
        }

        private TrackInfoCache CreateTrackInfoCache(SQLiteDataReader reader)
        {
            TrackInfoCache trackInfoCache = this.Deserialize<TrackInfoCache>(reader.GetBytes("bson"));
            trackInfoCache.Id = reader.GetInt64("id").ToString();
            return trackInfoCache;
        }

        private void ReloadSettings()
        {
            using (var reader = this.loadSettings.ExecuteReader())
            {
                while (reader.Read())
                {
                    this.Settings = this.Deserialize<CollectionSettings>(reader.GetBytes("bson"));
                    return;
                }
            }

            if (this.Settings == null)
            {
                this.Settings = CollectionSettings.CreateDefault();
                this.SaveSettings();
            }
        }

        public void Save(TrackInfoCache trackInfo)
        {
            this.saveTrackCache.Parameters["bson"].Value = trackInfo.ToBson(this.bsonWriterSettings);
            this.saveTrackCache.ExecuteNonQuery();
        }

        public void Save(Release release)
        {
            if (release.Id == null)
            {
                this.saveRelease.Parameters["bson"].Value = release.ToBson(this.bsonWriterSettings);
                this.saveRelease.ExecuteNonQuery();
                release.Id = this.connection.LastInsertRowId.ToString();
            }
            else
            {
                this.updateRelease.Parameters["id"].Value = long.Parse(release.Id);
                this.updateRelease.Parameters["bson"].Value = release.ToBson(this.bsonWriterSettings);
                this.updateRelease.ExecuteNonQuery();
            }
        }

        public void SaveSettings()
        {
            this.saveSettings.Parameters["bson"].Value = this.Settings.ToBson(this.bsonWriterSettings);
            this.saveSettings.ExecuteNonQuery();
        }

        public Release GetReleaseById(string id)
        {
            long longId;
            if (!long.TryParse(id, out longId))
            {
                return null;
            }

            this.loadReleaseById.Parameters["id"].Value = longId;
            using (var reader = this.loadReleaseById.ExecuteReader())
            {
                while (reader.Read())
                {
                    return this.CreateRelease(reader);
                }
            }
            return null;
        }

        public bool DeleteRelease(Release release, bool tryDeleteFiles)
        {
            this.deleteRelease.Parameters["id"].Value = release.Id;
            return this.deleteRelease.ExecuteNonQuery() > 0;
        }

        public Artist GetOrCreateArtist(string name)
        {
            return new Artist()
            {
                Name = name
            };
        }

        public void ClearCache()
        {
            this.ReloadSettings();
        }

        public void Dispose()
        {
            this.connection = null;
        }

        public ITransaction BeginTransaction()
        {
            return new SQLiteTransaction(this.connection);
        }
    }
}
