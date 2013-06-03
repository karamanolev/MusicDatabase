using System;
using System.Linq;
using System.Windows;
using MusicDatabase.Engine;
using MusicDatabase.Engine.ImportExport;

namespace MusicDatabase.Helpers
{
    class DirectoryImportHelper : HelperBase
    {
        public DirectoryImportHelper(MusicDatabaseWindow parentWindow, ICollectionSessionFactory collectionSessionFactory)
            : base(parentWindow, collectionSessionFactory)
        {
        }

        public void Run()
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                WaitWindow waitWindow = new WaitWindow("Importing collection...");
                waitWindow.ShowDialog(this.ParentWindow, () =>
                {
                    try
                    {
                        using (DirectoryCollectionImporter importer = new DirectoryCollectionImporter(folderDialog.SelectedPath, this.CollectionManager))
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
