using System;
using System.IO;
using System.Linq;
using System.Windows;
using MusicDatabase.Engine;
using MusicDatabase.Engine.ImportExport;

namespace MusicDatabase.Helpers
{
    class DirectoryExportHelper : HelperBase
    {
        public DirectoryExportHelper(MusicDatabaseWindow parentWindow, ICollectionSessionFactory collectionSessionFactory)
            : base(parentWindow, collectionSessionFactory)
        {
        }

        public void Run()
        {
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderDialog.ShowNewFolderButton = true;
            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = folderDialog.SelectedPath;

                if (Directory.GetFiles(path).Length != 0 || Directory.GetDirectories(path).Length != 0)
                {
                    MessageBoxResult emptyDirectoryResult = Dialogs.YesNoCancelQuestion("Target directory is not empty. Delete directory contents before exporting?");
                    if (emptyDirectoryResult == MessageBoxResult.Yes)
                    {
                        if (Directory.GetFiles(path, "*", SearchOption.AllDirectories).Any(f => Path.GetExtension(f).ToLower() != ".xml"))
                        {
                            Dialogs.Error("The directory contains files that aren't XML. I refuse to delete them!");
                            return;
                        }

                        if (!Utility.TryEmptyDirectory(path))
                        {
                            Dialogs.Error("Error deleting directory contents!");
                            return;
                        }
                    }
                    else if (emptyDirectoryResult == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                WaitWindow waitWindow = new WaitWindow("Exporting collection...");
                waitWindow.ShowDialog(this.ParentWindow, () =>
                {
                    using (DirectoryCollectionExporter exporter = new DirectoryCollectionExporter(folderDialog.SelectedPath, this.CollectionManager))
                    {
                        exporter.Export();
                    }
                });
            }
        }
    }
}
