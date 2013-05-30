using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace MusicDatabase.DiscogsLink
{
    /// <summary>
    /// Interaction logic for DiscogsReleaseIdTextBox.xaml
    /// </summary>
    public partial class DiscogsReleaseIdTextBox : UserControl
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(DiscogsReleaseIdTextBox), new UIPropertyMetadata(""));

        public DiscogsReleaseIdTextBox()
        {
            InitializeComponent();

            Binding binding = new Binding("Text");
            binding.Source = this;
            binding.Mode = BindingMode.TwoWay;
            this.textBox.SetBinding(TextBox.TextProperty, binding);
        }

        private void textBox_TextChanged(object sender, RoutedEventArgs e)
        {
            int releaseId = DiscogsUtility.GetReleaseId(this.textBox.Text);
            if (releaseId != 0)
            {
                this.textBox.SetCurrentValue(TextBox.TextProperty, releaseId.ToString());
            }
        }
    }
}
