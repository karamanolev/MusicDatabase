using System.Collections.Generic;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Local
{
    public class LocalAlbumArtist
    {
        public string Name { get; private set; }
        public Dictionary<string, LocalAlbum> Albums { get; private set; }

        public LocalAlbumArtist(TrackInfoCache trackInfo)
        {
            this.Name = trackInfo.AlbumArtist;
            this.Albums = new Dictionary<string, LocalAlbum>();
            this.Add(trackInfo);
        }

        public void Add(TrackInfoCache track)
        {
            LocalAlbum album;
            if (this.Albums.TryGetValue(track.Album, out album))
            {
                album.Add(track);
            }
            else
            {
                this.Albums[track.Album] = new LocalAlbum(track);
            }
        }
    }
}
