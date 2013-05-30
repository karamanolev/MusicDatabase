using System;
using System.Linq;

namespace MusicDatabase.Engine
{
    public class CollectionStatistics
    {
        public int TotalReleases { get; set; }
        public int TotalAlbumArtists { get; set; }
        public int TotalArtists { get; set; }
        public int TotalTracks { get; set; }

        public int PerfectReleases { get; set; }
        public double PerfectReleasesPart { get { return (double)this.PerfectReleases / this.TotalReleases; } }
        public int ReleasesWithImages { get; set; }
        public double ReleasesWithImagesPart { get { return (double)this.ReleasesWithImages / this.TotalReleases; } }

        public int TotalImages { get; set; }
        public long TotalImageBytes { get; set; }
        public double AverageImageBytesPerReleaseWithImages { get { return (double)this.TotalImageBytes / this.ReleasesWithImages; } }
        public double AverageImageBytesPerRelease { get { return (double)this.TotalImageBytes / this.TotalReleases; } }
        public int TotalAdditionalFiles { get; set; }
        public long TotalAdditionalFileBytes { get; set; }
        public int FlaggedReleases { get; set; }

    }
}
