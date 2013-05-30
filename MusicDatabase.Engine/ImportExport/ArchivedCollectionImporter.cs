using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.ImportExport
{
    public class ArchivedCollectionImporter : CollectionImporterBase
    {
        private ZipFile zipFile;

        public ArchivedCollectionImporter(string inputPath, CollectionManager collectionManager, Action<Release, ICollectionImageHandler> updateThumbnailAction)
            : base(collectionManager, updateThumbnailAction)
        {
            this.zipFile = new ZipFile(inputPath);
        }

        public override void Dispose()
        {
            this.zipFile.Close();

            base.Dispose();
        }

        public override IEnumerable<System.IO.Stream> GetReleaseStreams()
        {
            foreach (ZipEntry zipEntry in this.zipFile)
            {
                if (zipEntry.Name.StartsWith("Releases\\"))
                {
                    using (Stream stream = this.zipFile.GetInputStream(zipEntry))
                    {
                        yield return stream;
                    }
                }
            }
        }
    }
}
