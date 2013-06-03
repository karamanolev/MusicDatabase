using System;

namespace MusicDatabase.Engine.Entities
{
    public class EncodingTarget
    {
        public string TargetDirectory { get; set; }
        public string FileNamingPattern { get; set; }

        public EncodingTargetType Type { get; set; }
        public EncodingTargetMp3Settings Mp3Settings { get; set; }

        public string Extension
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
