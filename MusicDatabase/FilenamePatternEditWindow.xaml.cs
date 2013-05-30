using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for FilenamePatternEditWindow.xaml
    /// </summary>
    public partial class FilenamePatternEditWindow : MusicDatabaseWindow
    {
        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public FilenamePatternEditWindow()
        {
            InitializeComponent();

            Binding textBinding = new Binding("Text");
            textBinding.Source = this;
            textPattern.SetBinding(TextBox.TextProperty, textBinding);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(FilenamePatternEditWindow));
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

        private void btnSelector_Click(object sender, RoutedEventArgs e)
        {
            string selector = (sender as Button).Tag as string;
            int oldCaretIndex = textPattern.CaretIndex;
            Text = textPattern.Text.Insert(textPattern.CaretIndex, selector);
            textPattern.CaretIndex = oldCaretIndex + selector.Length;
        }

        private void OKCancelBox_OKClicked(object sender, System.EventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
