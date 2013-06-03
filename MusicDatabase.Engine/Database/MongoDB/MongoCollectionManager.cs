using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Database.MongoDB
{
    public class MongoCollectionManager : ICollectionManager
    {
        private const string SettingsCollectionName = "settings";
        private const string ReleasesCollectionName = "releases";
        private const string TrackInfosCollectionName = "trackInfos";

        private MongoDatabase database;
        private Dictionary<string, Artist> artistCache;

        private MongoCollection<CollectionSettings> settingsCollection;
        private MongoCollection<Release> releasesCollection;
        private MongoCollection<TrackInfoCache> trackInfosCollection;

        public ICollectionImageHandler ImageHandler { get; private set; }

        public IQueryable<Release> Releases
        {
            get
            {
                return this.releasesCollection.AsQueryable<Release>();
            }
        }

        public int ReleaseCount
        {
            get { return (int)this.releasesCollection.Count(); }
        }

        public IQueryable<TrackInfoCache> LocalTrackInfos
        {
            get
            {
                return this.trackInfosCollection.AsQueryable<TrackInfoCache>();
            }
        }

        public CollectionSettings Settings { get; private set; }

        public CollectionManagerOperations Operations { get; private set; }

        public MongoCollectionManager(MongoDatabase database, ICollectionImageHandler imageHandler)
        {
            this.database = database;
            this.releasesCollection = this.database.GetCollection<Release>(ReleasesCollectionName);
            this.settingsCollection = this.database.GetCollection<CollectionSettings>(SettingsCollectionName);
            this.trackInfosCollection = this.database.GetCollection<TrackInfoCache>(TrackInfosCollectionName);

            this.ImageHandler = imageHandler;
            this.Operations = new CollectionManagerOperations(this);

            this.artistCache = new Dictionary<string, Artist>();
            this.ReloadSettings();
        }

        private void ReloadSettings()
        {
            this.Settings = this.settingsCollection.FindOne();
            if (this.Settings == null)
            {
                this.Settings = CollectionSettings.CreateDefault();
                this.SaveSettings();
            }
        }

        public void SaveSettings()
        {
            this.settingsCollection.RemoveAll();
            this.settingsCollection.Save(this.Settings);
        }

        public void Save(Release release)
        {
            this.releasesCollection.Save(release);
        }

        public void Save(TrackInfoCache trackInfo)
        {
            this.trackInfosCollection.Save(trackInfo);
        }

        public void Dispose()
        {
            this.releasesCollection = null;
            this.settingsCollection = null;
            this.artistCache = null;
            this.database = null;
        }

        public Artist GetOrCreateArtist(string name)
        {
            return new Artist()
            {
                Name = name
            };

            /*Artist artist;

            this.artistCache.TryGetValue(name, out artist);

            if (artist == null)
            {
                artist = this.Artists.Where(a => a.Name == name).FirstOrDefault();
                this.artistCache[name] = artist;
            }

            if (artist == null)
            {
                artist = new Artist()
                {
                    Name = name
                };
                this.artistCache[name] = artist;
            }

            return artist;*/
        }

        public bool DeleteRelease(Release release, bool tryDeleteFiles)
        {
            bool successfullyDeleted = true;

            foreach (var image in release.Images)
            {
                try
                {
                    this.ImageHandler.DeleteImage(image);
                }
                catch (Exception e)
                {
                    Utility.WriteToErrorLog(e.ToString());
                    successfullyDeleted = false;
                }
            }

            if (tryDeleteFiles)
            {
                foreach (var track in release.Tracklist)
                {
                    string absolutePath = Path.Combine(this.Settings.MusicDirectory, track.RelativeFilename);
                    successfullyDeleted = Utility.TryDeleteFile(absolutePath) && successfullyDeleted;
                    successfullyDeleted = Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(absolutePath)) && successfullyDeleted;
                }
            }

            this.releasesCollection.Remove(Query.EQ("_id", ObjectId.Parse(release.Id)));

            return successfullyDeleted;
        }

        public void ClearCache()
        {
            this.ReloadSettings();
        }


        public Release GetReleaseById(string id)
        {
            ObjectId objectId;
            if (!ObjectId.TryParse(id, out objectId))
            {
                return null;
            }
            return this.releasesCollection.FindOne(Query.EQ("_id", objectId));
        }
    }
}
