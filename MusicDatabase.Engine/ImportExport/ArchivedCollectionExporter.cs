using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;

namespace MusicDatabase.Engine.ImportExport
{
    public class ArchivedCollectionExporter : CollectionExporterBase
    {
        private ZipOutputStream zipOutputStream;

        public ArchivedCollectionExporter(string outputPath, ICollectionManager collectionManager)
            : base(collectionManager)
        {
            this.zipOutputStream = new ZipOutputStream(File.Create(outputPath));
            this.zipOutputStream.SetLevel(9);
        }

        protected override IEnumerable<Stream> GetEntryOutputStream(string entryName, DateTime dateModified, object obj)
        {
            ZipEntry zipEntry = new ZipEntry(entryName);
            zipEntry.DateTime = dateModified;
            zipEntry.IsUnicodeText = true;
            this.zipOutputStream.PutNextEntry(zipEntry);

            yield return this.zipOutputStream;
        }

        public override void Dispose()
        {
            this.zipOutputStream.Finish();
            this.zipOutputStream.Close();
            this.zipOutputStream.Dispose();

            base.Dispose();
        }
    }
}
