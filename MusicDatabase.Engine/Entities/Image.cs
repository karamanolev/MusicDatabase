using FluentNHibernate.Mapping;

namespace MusicDatabase.Engine.Entities
{
    public class Image
    {
        public class ImageMap : ClassMap<Image>
        {
            public ImageMap()
            {
                Id(x => x.Id);
                Map(x => x.IsMain);
                Map(x => x.Type);
                Map(x => x.MimeType);
                Map(x => x.Extension);
                Map(x => x.Description);
            }
        }

        public virtual int Id { get; protected set; }
        public virtual bool IsMain { get; set; }
        public virtual ImageType Type { get; set; }
        public virtual string MimeType { get; set; }
        public virtual string Extension { get; set; }
        public virtual string Description { get; set; }

        public Image()
        {
        }
    }
}
