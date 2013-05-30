using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    public partial class ViewReleaseFilesWindow : MusicDatabaseWindow
    {
        public ViewReleaseFilesWindow(Release release)
        {
            InitializeComponent();

            this.filesEditor.Release = release;
        }
    }
}
