using System;
using System.IO;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.ImportExport
{
    public class XmlReleaseImporter : CollectionImporterBase
    {
        private string path;

        public XmlReleaseImporter(string path, ICollectionManager collectionManager)
            : base(collectionManager)
        {
            this.path = path;
        }

        public Release ImportRelease()
        {
            using (Stream inputStream = File.OpenRead(path))
            {
                return this.ImportRelease(inputStream);
            }
        }
    }
}
