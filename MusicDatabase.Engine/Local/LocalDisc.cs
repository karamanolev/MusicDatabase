using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Local
{
    public class LocalDisc
    {
        public TrackInfoCache[] Tracks { get; private set; }

        public LocalDisc(TrackInfoCache trackInfo)
        {
            this.Tracks = new TrackInfoCache[trackInfo.TrackCount];
            this.Tracks[trackInfo.Track - 1] = trackInfo;
        }

        public void Add(TrackInfoCache t)
        {
            Assert.IsTrue(this.Tracks[t.Track - 1] == null);
            this.Tracks[t.Track - 1] = t;
        }
    }
}
