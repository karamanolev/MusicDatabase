using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace MusicDatabase.Engine.Entities
{
    public class TrackInfoCache
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Filename { get; set; }
        public long LastWriteTime { get; set; }

        public string RelativeFilename { get; set; }
        public string Artist { get; set; }
        public string AlbumArtist { get; set; }
        public string Album { get; set; }
        public int Disc { get; set; }
        public int DiscCount { get; set; }
        public int Track { get; set; }
        public int TrackCount { get; set; }
        public string Title { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }

        public TrackInfoCache()
        {
            this.Id = ObjectId.GenerateNewId().ToString();
        }
    }
}
