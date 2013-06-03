using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MusicDatabase.Engine.Entities
{
    public class CollectionSettings
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string MusicDirectory { get; set; }
        public string FileNamingPattern { get; set; }
        public bool NetworkEncoding { get; set; }
        public bool ShowImagesInReleaseTree { get; set; }
        public ReleasesViewMode ReleasesViewMode { get; set; }
        public List<EncodingTarget> EncodingTargets { get; set; }

        public CollectionSettings()
        {
            this.Id = ObjectId.GenerateNewId().ToString();
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
