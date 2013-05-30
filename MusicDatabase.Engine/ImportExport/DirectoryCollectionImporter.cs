using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.ImportExport
{
    public class DirectoryCollectionImporter : CollectionImporterBase
    {
        private string path;

        public DirectoryCollectionImporter(string path, CollectionManager collectionManager, Action<Release, ICollectionImageHandler> updateThumbnailAction)
            : base(collectionManager, updateThumbnailAction)
        {
            this.path = path;
        }

        public override IEnumerable<Stream> GetReleaseStreams()
        {
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                string directoryName = Path.GetFileName(Path.GetDirectoryName(file));
                if (directoryName == "Releases")
                {
                    using (Stream stream = File.OpenRead(file))
                    {
                        yield return stream;
                    }
                }
            }
        }
    }
}
