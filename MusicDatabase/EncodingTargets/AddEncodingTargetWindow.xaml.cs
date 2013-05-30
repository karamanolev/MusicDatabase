using System.Windows;

namespace MusicDatabase.EncodingTargets
{
    /// <summary>
    /// Interaction logic for AddEncodingTargetWindow.xaml
    /// </summary>
    public partial class AddEncodingTargetWindow : Window
    {
        public string TargetDirectory
        {
            get { return this.textMusicDirectory.Text; }
            set { this.textMusicDirectory.Text = value; }
        }

        public string FileNamingPattern
        {
            get { return this.textFileNamingPattern.Text; }
            set { this.textFileNamingPattern.Text = value; }
        }

        public int VbrQuality
        {
            get { return this.comboVbrQuality.SelectedIndex; }
            set { this.comboVbrQuality.SelectedIndex = value; }
        }

        public AddEncodingTargetWindow()
        {
            InitializeComponent();
        }

        private void OKCancelBox_OKClicked(object sender, System.EventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
