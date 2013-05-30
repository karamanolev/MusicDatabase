using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for FilenamePatternSelector.xaml
    /// </summary>
    public partial class FilenamePatternSelector : UserControl
    {
        public FilenamePatternSelector()
        {
            InitializeComponent();

            Binding comboBinding = new Binding("Text");
            comboBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            comboBinding.Source = this;
            comboBinding.Mode = BindingMode.TwoWay;
            comboPatterns.SetBinding(ComboBox.TextProperty, comboBinding);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(FilenamePatternSelector), new PropertyMetadata(""));
        public string Text
        {
            get
            {
                return (string)GetValue(TextProperty);
            }
            set
            {
                SetValue(TextProperty, value);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            FilenamePatternEditWindow editWindow = new FilenamePatternEditWindow();
            editWindow.Text = this.Text;
            if (editWindow.ShowDialog(Window.GetWindow(this)) == true)
            {
                this.Text = editWindow.Text;
            }
        }
    }
}
