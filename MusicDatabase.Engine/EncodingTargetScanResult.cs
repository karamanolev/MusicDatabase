using System;
using System.Collections.Generic;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public class EncodingTargetScanResult
    {
        public Dictionary<Release, List<Track>> TracksToEncode { get; set; }
        public HashSet<Release> ReleasesToEncode { get; set; }
        public string[] FilesToDelete { get; set; }

        public int ReleasesToEncodeTrackCount
        {
            get
            {
                return this.ReleasesToEncode.Sum(i => i.Tracklist.Count);
            }
        }
    }
}
