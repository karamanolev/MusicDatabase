using System;
using System.Linq;
using Microsoft.Win32;
using MusicDatabase.Engine;
using MusicDatabase.Engine.ImportExport;

namespace MusicDatabase.Helpers
{
    class ArchivedExportHelper : HelperBase
    {
        public ArchivedExportHelper(MusicDatabaseWindow parentWindow, ICollectionSessionFactory collectionSessionFactory)
            : base(parentWindow, collectionSessionFactory)
        {
        }

        public void Run()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Zip Files (*.zip)|*.zip|" + Utility.AllFilesFilter;
            saveFileDialog.FileName = "Export_" + DateTime.Now.ToString("yyyy_MM_dd") + ".zip";
            if (saveFileDialog.ShowDialog() == true)
            {
                WaitWindow waitWindow = new WaitWindow("Exporting collection...");
                waitWindow.ShowDialog(this.ParentWindow, () =>
                {
                    using (CollectionExporterBase exporter = new ArchivedCollectionExporter(saveFileDialog.FileName, this.CollectionManager))
                    {
                        exporter.Export();
                    }
                });
            }
        }
    }
}
