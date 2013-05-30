using System;
using System.Collections.Generic;
using FluentNHibernate.Mapping;

namespace MusicDatabase.Settings.Entities
{
    public class MusicDatabaseSettings
    {
        public class MusicDatabaseSettingsMap : ClassMap<MusicDatabaseSettings>
        {
            public MusicDatabaseSettingsMap()
            {
                Id(x => x.Id);
                Map(x => x.CollectionDatabasePath);
                Map(x => x.LocalConcurrencyLevel);
                HasMany(x => x.WindowsSettings).AsBag().Cascade.All();
            }
        }

        public virtual int Id { get; protected set; }
        public virtual string CollectionDatabasePath { get; set; }
        public virtual int LocalConcurrencyLevel { get; set; }
        public virtual IList<WindowSettings> WindowsSettings { get; protected set; }

        public virtual int ActualLocalConcurrencyLevel
        {
            get
            {
                if (this.LocalConcurrencyLevel != 0)
                {
                    return this.LocalConcurrencyLevel;
                }
                return Environment.ProcessorCount;
            }
        }

        public MusicDatabaseSettings()
        {
            this.WindowsSettings = new List<WindowSettings>();
        }

        public static MusicDatabaseSettings CreateDefault()
        {
            return new MusicDatabaseSettings()
            {
            };
        }
    }
}
