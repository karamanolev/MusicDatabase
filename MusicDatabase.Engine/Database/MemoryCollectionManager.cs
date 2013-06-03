using System;
using System.Collections.Generic;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Database
{
    class MemoryCollectionManager : ICollectionManager
    {
        private List<Release> releases;
        private List<TrackInfoCache> localTrackInfos;

        public ICollectionImageHandler ImageHandler { get; private set; }
        public IQueryable<Release> Releases
        {
            get { return this.releases.AsQueryable<Release>(); }
        }
        public int ReleaseCount
        {
            get { return this.releases.Count; }
        }

        public IQueryable<TrackInfoCache> LocalTrackInfos
        {
            get { return this.localTrackInfos.AsQueryable<TrackInfoCache>(); }
        }
        public CollectionSettings Settings { get; private set; }
        public CollectionManagerOperations Operations { get; private set; }

        public MemoryCollectionManager(ICollectionImageHandler imageHandler)
        {
            this.ImageHandler = imageHandler;
            this.Operations = new CollectionManagerOperations(this);
            this.Settings = new CollectionSettings();
        }

        public void ClearCache()
        {
        }

        public void Save(TrackInfoCache trackInfo)
        {
            if (!this.localTrackInfos.Any(t => t.Id.Equals(trackInfo.Id)))
            {
                this.localTrackInfos.Add(trackInfo);
            }
        }

        public void SaveSettings()
        {
        }

        public void Save(Release release)
        {
            if (!this.releases.Any(r => r.Id.Equals(release.Id)))
            {
                this.releases.Add(release);
            }
        }

        public bool DeleteRelease(Release release, bool tryDeleteFiles)
        {
            Release currentRelease = this.releases.Where(r => r.Id.Equals(release.Id)).FirstOrDefault();
            if (currentRelease != null)
            {
                this.releases.Remove(currentRelease);
            }
            return true;
        }

        public Artist GetOrCreateArtist(string name)
        {
            return new Artist()
            {
                Name = name
            };
        }

        public void Dispose()
        {
            this.releases = null;
            this.localTrackInfos = null;
        }


        public Release GetReleaseById(string id)
        {
            return this.releases.FirstOrDefault(r => r.Id.Equals(id));
        }
    }
}
