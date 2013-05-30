using System.Collections.Generic;
using FluentNHibernate.Mapping;

namespace MusicDatabase.Engine.Entities
{
    public class CollectionSettings
    {
        public class CollectionSettingsMap : ClassMap<CollectionSettings>
        {
            public CollectionSettingsMap()
            {
                Id(x => x.Id);
                Map(x => x.MusicDirectory);
                Map(x => x.FileNamingPattern);
                Map(x => x.NetworkEncoding);
                Map(x => x.ReleasesViewMode);
                Map(x => x.ShowImagesInReleaseTree);
                HasMany(x => x.EncodingTargets).AsList(x => x.Column("Pos")).Cascade.AllDeleteOrphan();
            }
        }

        public virtual int Id { get; protected set; }
        public virtual string MusicDirectory { get; set; }
        public virtual string FileNamingPattern { get; set; }
        public virtual bool NetworkEncoding { get; set; }
        public virtual bool ShowImagesInReleaseTree { get; set; }
        public virtual ReleasesViewMode ReleasesViewMode { get; set; }
        public virtual IList<EncodingTarget> EncodingTargets { get; protected set; }

        public CollectionSettings()
        {
            this.EncodingTargets = new List<EncodingTarget>();
        }

        public static CollectionSettings CreateDefault()
        {
            return new CollectionSettings()
            {
                ShowImagesInReleaseTree = true
            };
        }
    }
}
