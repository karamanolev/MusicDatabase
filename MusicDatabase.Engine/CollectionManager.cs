using System.Linq;
using MusicDatabase.Engine.Entities;
using NHibernate;
using NHibernate.Linq;
using System;
using System.IO;
using System.Collections.Generic;

namespace MusicDatabase.Engine
{
    public class CollectionManager : IDisposable
    {
        private CollectionSettings settings;
        private ISession session;
        private Dictionary<string, Artist> artistCache;

        public ICollectionImageHandler ImageHandler { get; private set; }

        public IQueryable<Release> Releases
        {
            get
            {
                return this.session.Query<Release>();
            }
        }

        public IQueryable<Artist> Artists
        {
            get
            {
                return this.session.Query<Artist>();
            }
        }

        public IQueryable<TrackInfoCache> LocalTrackInfos
        {
            get
            {
                return this.session.Query<TrackInfoCache>();
            }
        }

        public CollectionSettings Settings
        {
            get
            {
                if (this.settings == null)
                {
                    this.settings = this.session.Query<CollectionSettings>().FirstOrDefault();
                }
                return this.settings;
            }
            set
            {
                CollectionSettings oldSettings = this.Settings;

                this.settings = value;

                if (oldSettings != null)
                {
                    this.session.Delete(oldSettings);
                }

                using (var transaction = this.BeginTransaction())
                {
                    this.session.SaveOrUpdate(this.settings);
                    transaction.Commit();
                }
            }
        }

        public CollectionManagerOperations Operations { get; private set; }

        public CollectionManager(ICollectionSessionFactory sessionFactory)
        {
            this.ImageHandler = sessionFactory.CreateImageHandler();

            this.session = sessionFactory.CreateSession();
            this.Operations = new CollectionManagerOperations(this);

            this.artistCache = new Dictionary<string, Artist>();
        }

        public CollectionManager()
        {
        }

        public void SaveSettings()
        {
            if (this.settings != null)
            {
                using (var transaction = this.BeginTransaction())
                {
                    this.session.SaveOrUpdate(this.settings);
                    transaction.Commit();
                }
            }
        }

        public void SaveOrUpdate(object obj)
        {
            this.session.SaveOrUpdate(obj);
        }

        public void Delete(object obj)
        {
            this.session.Delete(obj);
        }

        public void Dispose()
        {
            if (this.session != null)
            {
                this.session.Dispose();
                this.session = null;
            }
        }

        public ITransaction BeginTransaction()
        {
            return this.session.BeginTransaction();
        }

        public Artist GetOrCreateArtist(string name)
        {
            Artist artist;
            
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

            return artist;
        }

        public bool DeleteRelease(Release release)
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
            foreach (var track in release.Tracklist)
            {
                string absolutePath = Path.Combine(this.Settings.MusicDirectory, track.RelativeFilename);
                successfullyDeleted = Utility.TryDeleteFile(absolutePath) && successfullyDeleted;
                successfullyDeleted = Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(absolutePath)) && successfullyDeleted;
            }

            this.Delete(release);

            return successfullyDeleted;
        }

        public void RemoveRelease(Release release)
        {
            this.Delete(release);
        }

        public void ClearCache()
        {
            this.session.Clear();
            this.settings = null;
        }

        public static event EventHandler CollectionChanged;
        public static void OnCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(null, EventArgs.Empty);
            }
        }
    }
}
