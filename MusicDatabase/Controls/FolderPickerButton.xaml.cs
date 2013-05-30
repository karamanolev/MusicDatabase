using System.Windows;
using System.Windows.Controls;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for FolderPickerButton.xaml
    /// </summary>
    public partial class FolderPickerButton : UserControl
    {
        public string SelectedPath
        {
            get { return (string)GetValue(SelectedPathProperty); }
            set { SetValue(SelectedPathProperty, value); }
        }
        public static readonly DependencyProperty SelectedPathProperty =
            DependencyProperty.Register("SelectedPath", typeof(string), typeof(FolderPickerButton), new UIPropertyMetadata(""));
        

        public FolderPickerButton()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.ShowNewFolderButton = true;

            if (!string.IsNullOrEmpty(this.SelectedPath))
            {
                dialog.SelectedPath = this.SelectedPath;
            }

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.SelectedPath = dialog.SelectedPath;
            }
        }
    }
}
