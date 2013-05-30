using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Local
{
    public class LocalAlbum
    {
        public string Title { get; set; }
        public string AlbumArtist { get; set; }
        public int Year { get; set; }
        public int DiscCount { get; set; }
        public string Genre { get; set; }
        public LocalDisc[] Discs { get; set; }

        public LocalAlbum(TrackInfoCache trackInfo)
        {
            this.Title = trackInfo.Album;
            this.DiscCount = trackInfo.DiscCount;
            this.Year = trackInfo.Year;
            this.Genre = trackInfo.Genre;
            this.AlbumArtist = trackInfo.AlbumArtist;

            this.Discs = new LocalDisc[trackInfo.DiscCount];
            this.Discs[trackInfo.Disc - 1] = new LocalDisc(trackInfo);
        }

        public void Add(TrackInfoCache t)
        {
            //Assert.IsTrue(this.Title == t.Album);
            //Assert.IsTrue(this.AlbumArtist == t.AlbumArtist);
            //Assert.IsTrue(this.DiscCount == t.DiscCount);
            //Assert.IsTrue(this.Year == t.Year);
            //Assert.IsTrue(this.Genre == t.Genre);

            if (this.Discs[t.Disc - 1] == null)
            {
                this.Discs[t.Disc - 1] = new LocalDisc(t);
            }
            else
            {
                this.Discs[t.Disc - 1].Add(t);
            }
        }

        public override string ToString()
        {
            return this.AlbumArtist + " - " + this.Title + " [" + this.Year + "]";
        }
    }
}
