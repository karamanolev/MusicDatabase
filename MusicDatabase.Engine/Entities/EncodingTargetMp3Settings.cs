using FluentNHibernate.Mapping;

namespace MusicDatabase.Engine.Entities
{
    public class EncodingTargetMp3Settings
    {
        public class EncodingTargetMp3SettingsMap : ComponentMap<EncodingTargetMp3Settings>
        {
            public EncodingTargetMp3SettingsMap()
            {
                Map(x => x.VbrQuality);
            }
        }

        public virtual int VbrQuality { get; set; }
    }
}
