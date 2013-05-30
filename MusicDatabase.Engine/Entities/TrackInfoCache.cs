using FluentNHibernate.Mapping;

namespace MusicDatabase.Engine.Entities
{
    public class TrackInfoCache
    {
        public class TrackInfoCacheMap : ClassMap<TrackInfoCache>
        {
            public TrackInfoCacheMap()
            {
                Id(x => x.Id);
                Map(x => x.Filename);
                Map(x => x.LastWriteTime);

                Map(x => x.RelativeFilename);
                Map(x => x.Artist);
                Map(x => x.AlbumArtist);
                Map(x => x.Album);
                Map(x => x.Disc);
                Map(x => x.DiscCount);
                Map(x => x.Track);
                Map(x => x.TrackCount);
                Map(x => x.Title);
                Map(x => x.Genre);
                Map(x => x.Year);
            }
        }

        public virtual int Id { get; protected set; }
        public virtual string Filename { get; set; }
        public virtual long LastWriteTime { get; set; }

        public virtual string RelativeFilename { get; set; }
        public virtual string Artist { get; set; }
        public virtual string AlbumArtist { get; set; }
        public virtual string Album { get; set; }
        public virtual int Disc { get; set; }
        public virtual int DiscCount { get; set; }
        public virtual int Track { get; set; }
        public virtual int TrackCount { get; set; }
        public virtual string Title { get; set; }
        public virtual string Genre { get; set; }
        public virtual int Year { get; set; }
    }
}
