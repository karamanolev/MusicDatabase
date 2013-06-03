using System;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public interface ICollectionManager : IDisposable
    {
        ICollectionImageHandler ImageHandler { get; }

        IQueryable<Release> Releases { get; }
        int ReleaseCount { get; }

        IQueryable<TrackInfoCache> LocalTrackInfos { get; }

        CollectionSettings Settings { get; }
        CollectionManagerOperations Operations { get; }

        void Save(TrackInfoCache trackInfo);
        void Save(Release release);

        void SaveSettings();

        Release GetReleaseById(string id);
        bool DeleteRelease(Release release, bool tryDeleteFiles);

        Artist GetOrCreateArtist(string name);

        void ClearCache();
    }
}
