using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusicDatabase.Engine.ImportExport
{
    public class DirectoryCollectionExporter : CollectionExporterBase
    {
        private string outputPath;

        public DirectoryCollectionExporter(string outputPath, CollectionManager collectionManager)
            : base(collectionManager)
        {
            this.outputPath = outputPath;
        }

        protected override IEnumerable<Stream> GetEntryOutputStream(string entryName, DateTime dateModified, object obj)
        {
            string targetName = Path.Combine(outputPath, entryName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetName));
            using (Stream outputStream = File.Create(targetName))
            {
                yield return outputStream;
            }
            File.SetLastWriteTime(targetName, dateModified);
        }
    }
}
