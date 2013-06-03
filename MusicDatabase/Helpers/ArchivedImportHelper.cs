using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using MusicDatabase.Engine;
using MusicDatabase.Engine.ImportExport;

namespace MusicDatabase.Helpers
{
    class ArchivedImportHelper : HelperBase
    {
        public ArchivedImportHelper(MusicDatabaseWindow parentWindow, ICollectionSessionFactory collectionSessionFactory)
            : base(parentWindow, collectionSessionFactory)
        {
        }

        public void Run()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Zip Files (*.zip)|*.zip|" + Utility.AllFilesFilter;
            if (openFileDialog.ShowDialog() == true)
            {
                WaitWindow waitWindow = new WaitWindow("Importing collection...");
                waitWindow.ShowDialog(this.ParentWindow, () =>
                {
                    try
                    {
                        using (CollectionImporterBase importer = new ArchivedCollectionImporter(openFileDialog.FileName, this.CollectionManager))
                        {
                            importer.Import();
                        }
                    }
                    catch (Exception ex)
                    {
                        Utility.WriteToErrorLog("Error importing: " + ex.ToString());
                        MessageBox.Show("Error importing backup: " + ex.Message);
                    }

                    this.ParentWindow.Dispatcher.BeginInvokeAction(() =>
                    {
                        CollectionManagerGlobal.OnCollectionChanged();
                    });
                });
            }
        }
    }
}
