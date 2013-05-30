using System.Windows;
using DiscogsNet.Model;

namespace MusicDatabase.DiscogsLink
{
    /// <summary>
    /// Interaction logic for DiscogsSelectLabelWindow.xaml
    /// </summary>
    public partial class DiscogsSelectLabelWindow : Window
    {
        public ReleaseLabel SelectedLabel
        {
            get
            {
                return (ReleaseLabel)this.comboLabel.SelectedItem;
            }
        }

        public DiscogsSelectLabelWindow(Release release)
        {
            InitializeComponent();

            this.comboLabel.ItemsSource = release.Labels;
        }

        private void OKCancelBox_OKClicked(object sender, System.EventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
