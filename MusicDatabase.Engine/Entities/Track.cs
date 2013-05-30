using System.Collections.Generic;
using FluentNHibernate.Mapping;
using System.IO;

namespace MusicDatabase.Engine.Entities
{
    public class Track
    {
        public class TrackMap : ClassMap<Track>
        {
            public TrackMap()
            {
                Id(x => x.Id);
                Map(x => x.Disc);
                Map(x => x.Position);
                HasMany(x => x.Artists).AsList(x => x.Column("Pos")).Cascade.AllDeleteOrphan().LazyLoad();
                Map(x => x.JoinedArtists);
                Map(x => x.Title);
                Map(x => x.RelativeFilename);
                Map(x => x.DynamicRange);
                Map(x => x.ReplayGainTrackGain);
                Map(x => x.ReplayGainTrackPeak);
            }
        }

        public virtual int Id { get; protected set; }
        public virtual int Disc { get; set; }
        public virtual int Position { get; set; }
        public virtual IList<TrackArtist> Artists { get; protected set; }
        public virtual string JoinedArtists { get; set; }
        public virtual string Title { get; set; }
        public virtual string RelativeFilename { get; set; }

        public virtual double DynamicRange { get; set; }
        public virtual double ReplayGainTrackGain { get; set; }
        public virtual double ReplayGainTrackPeak { get; set; }

        public Track()
        {
            this.Artists = new List<TrackArtist>();
        }

        public virtual string GetAbsoluteFilename(CollectionManager collectionManager)
        {
            return Path.Combine(collectionManager.Settings.MusicDirectory, this.RelativeFilename);
        }
    }
}
