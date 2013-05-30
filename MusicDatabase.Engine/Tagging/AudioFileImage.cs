using MusicDatabase.Engine.Entities;
namespace MusicDatabase.Engine.Tagging
{
    public class AudioFileImage
    {
        public byte[] Data { get; set; }
        public ImageType Type { get; set; }
        public string Description { get; set; }
        public string MimeType { get; set; }
        public string Extension { get; set; }
    }
}
