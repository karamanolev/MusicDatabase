using System;
using System.Collections.Generic;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Local
{
    public class LocalCollectionMatcherResult
    {
        public List<Release> UnchangedReleases { get; private set; }
        public HashSet<Release> DeletedReleases { get; private set; }
        public HashSet<Tuple<Release, LocalAlbum>> ChangedReleases { get; private set; }
        public List<LocalAlbum> NewReleases { get; private set; }

        public LocalCollectionMatcherResult()
        {
            this.UnchangedReleases = new List<Release>();
            this.DeletedReleases = new HashSet<Release>();
            this.ChangedReleases = new HashSet<Tuple<Release, LocalAlbum>>();
            this.NewReleases = new List<LocalAlbum>();
        }
    }
}
