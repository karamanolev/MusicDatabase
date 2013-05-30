using FluentNHibernate.Mapping;

namespace MusicDatabase.Engine.Entities
{
    public class Artist
    {
        public class ArtistMap : ClassMap<Artist>
        {
            ArtistMap()
            {
                Id(x => x.Id);
                Map(x => x.Name);
            }
        }

        public virtual int Id { get; protected set; }
        public virtual string Name { get; set; }
    }
}
