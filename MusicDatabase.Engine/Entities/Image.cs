using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MusicDatabase.Engine.Entities
{
    public class Image
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public bool IsMain { get; set; }
        public ImageType Type { get; set; }
        public string MimeType { get; set; }
        public string Extension { get; set; }
        public string Description { get; set; }

        public Image()
        {
            this.Id = ObjectId.GenerateNewId().ToString();
        }
    }
}
