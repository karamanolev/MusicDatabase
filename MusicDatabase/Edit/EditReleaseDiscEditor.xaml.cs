using System.Linq;
using System.Windows.Controls;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Edit
{
    /// <summary>
    /// Interaction logic for EditReleaseDiscEditor.xaml
    /// </summary>
    public partial class EditReleaseDiscEditor : UserControl
    {
        public EditReleaseDiscEditor()
        {
            InitializeComponent();
        }

        public void SetData(Release release, int discNumber)
        {
            this.dataGrid.ItemsSource = release.Tracklist.Where(t => t.Disc == discNumber).ToArray();
        }
    }
}
