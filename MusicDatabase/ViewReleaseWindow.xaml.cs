using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.ImportExport;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ViewReleaseWindow.xaml
    /// </summary>
    public partial class ViewReleaseWindow : MusicDatabaseWindow
    {
        private Release release;

        public ViewReleaseWindow()
            : base(new CollectionSessionFactory_SQLiteMemory())
        {
            InitializeComponent();

            this.releaseDetails.CollectionManager = this.CollectionManager;
        }

        private void UpdateUI()
        {
            this.releaseDetails.Release = this.release;
            this.tracklistView.Releases = new Release[] { this.release };
        }

        public void LoadReleaseFromXml(string path)
        {
            this.busyIndicator.IsBusy = true;

            new Task(() =>
            {
                using (XmlReleaseImporter xmlReleaseImporter = new XmlReleaseImporter(path, this.CollectionManager, UIHelper.UpdateReleaseThumbnail))
                {
                    this.release = xmlReleaseImporter.ImportRelease();
                }

                this.Dispatcher.InvokeAction(() =>
                {
                    this.releaseDetails.Visibility = Visibility.Visible;
                    this.tracklistView.Visibility = Visibility.Visible;
                    this.busyIndicator.IsBusy = false;

                    this.UpdateUI();
                });
            }).Start();
        }
    }
}
