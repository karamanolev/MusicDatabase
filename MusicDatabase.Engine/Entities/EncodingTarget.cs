using FluentNHibernate.Mapping;
using System;

namespace MusicDatabase.Engine.Entities
{
    public class EncodingTarget
    {
        public class EncodingTargetMap : ClassMap<EncodingTarget>
        {
            public EncodingTargetMap()
            {
                Id(x => x.Id);
                Map(x => x.TargetDirectory);
                Map(x => x.FileNamingPattern);

                Map(x => x.Type);
                Component(x => x.Mp3Settings).ColumnPrefix("Mp3_");
            }
        }

        public virtual int Id { get; protected set; }
        public virtual string TargetDirectory { get; set; }
        public virtual string FileNamingPattern { get; set; }

        public virtual EncodingTargetType Type { get; set; }
        public virtual EncodingTargetMp3Settings Mp3Settings { get; set; }

        public virtual string Extension
        {
            get
            {
                if (this.Type == EncodingTargetType.Mp3)
                {
                    return ".mp3";
                }
                else
                {
                    throw new NotSupportedException("Unsupported EncodingTargetType " + this.Type);
                }
            }
        }
    }
}
