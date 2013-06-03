using System.Collections.Generic;
using System.IO;

namespace MusicDatabase.Engine.Entities
{
    public class Track
    {
        public int Disc { get; set; }
        public int Position { get; set; }
        public List<TrackArtist> Artists { get; set; }
        public string JoinedArtists { get; set; }
        public string Title { get; set; }
        public string RelativeFilename { get; set; }

        public double DynamicRange { get; set; }
        public double ReplayGainTrackGain { get; set; }
        public double ReplayGainTrackPeak { get; set; }

        public Track()
        {
            this.Artists = new List<TrackArtist>();
        }

        public string GetAbsoluteFilename(ICollectionManager collectionManager)
        {
            return Path.Combine(collectionManager.Settings.MusicDirectory, this.RelativeFilename);
        }
    }
}
