using System.Collections.Generic;
using System.IO;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ViewReleaseImagesWindow.xaml
    /// </summary>
    public partial class ViewReleaseImagesWindow : MusicDatabaseWindow
    {
        private Release release;

        public ViewReleaseImagesWindow(ICollectionManager collectionManager, Release release)
        {
            this.release = release;
            this.CollectionManager = collectionManager;

            InitializeComponent();

            this.releaseImagesEditor.CollectionManager = this.CollectionManager;
            this.releaseImagesEditor.Release = release;
        }

        private void btnSaveAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowser = new System.Windows.Forms.FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Dictionary<ImageType, int> counts = new Dictionary<ImageType, int>();
                foreach (Image image in this.release.Images)
                {
                    int number;
                    if (!counts.TryGetValue(image.Type, out number))
                    {
                        number = 0;
                    }
                    ++number;
                    counts[image.Type] = number;

                    string name = Utility.PascalCaseToString(image.Type.ToString()) + " " + number + image.Extension;
                    File.WriteAllBytes(Path.Combine(folderBrowser.SelectedPath, name), this.CollectionManager.ImageHandler.LoadImage(image));
                }

                Dialogs.Inform("Images saved successfully.");
            }
        }
    }
}
