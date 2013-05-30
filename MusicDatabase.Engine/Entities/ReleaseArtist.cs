using FluentNHibernate.Mapping;

namespace MusicDatabase.Engine.Entities
{
    public class ReleaseArtist : IReleaseArtist
    {
        public class ReleaseArtistMap : ClassMap<ReleaseArtist>
        {
            public ReleaseArtistMap()
            {
                Id(x => x.Id);
                References(x => x.Artist).Cascade.SaveUpdate();
                Map(x => x.JoinString);
            }
        }

        public virtual int Id { get; protected set; }
        public virtual Artist Artist { get; set; }
        public virtual string JoinString { get; set; }
    }
}
