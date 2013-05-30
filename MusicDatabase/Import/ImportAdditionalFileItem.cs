using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Import
{
    public class ImportAdditionalFileItem
    {
        public ReleaseAdditionalFileType Type { get; set; }
        public string Extension { get; set; }
        public byte[] Data { get; set; }
    }
}
