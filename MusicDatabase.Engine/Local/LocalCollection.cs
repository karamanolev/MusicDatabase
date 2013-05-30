using System.Collections.Generic;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Local
{
    public class LocalCollection
    {
        public Dictionary<string, LocalAlbumArtist> AlbumArtists;

        public LocalCollection()
        {
            this.AlbumArtists = new Dictionary<string, LocalAlbumArtist>();
        }

        public void Add(TrackInfoCache track)
        {
            this.PreprocessTrack(track);

            LocalAlbumArtist albumArtist;
            if (this.AlbumArtists.TryGetValue(track.AlbumArtist, out albumArtist))
            {
                albumArtist.Add(track);
            }
            else
            {
                this.AlbumArtists[track.AlbumArtist] = new LocalAlbumArtist(track);
            }
        }

        private void PreprocessTrack(TrackInfoCache track)
        {
            int i = track.Album.Length - 1;
            while (i >= 0 && char.IsDigit(track.Album[i])) --i;
            --i;

            if (i > 0 && i + 2 < track.Album.Length && track.Album.Substring(i, 2) == "CD")
            {
                Assert.IsTrue(int.Parse(track.Album.Substring(i + 2)) == track.Disc);
                track.Album = track.Album.Substring(0, i - 1);
            }
        }
    }
}
