using System;
using System.Windows;
using System.Windows.Controls;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for IconButton.xaml
    /// </summary>
    public partial class IconButton : UserControl
    {
        public Image Icon
        {
            get { return (Image)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register("Icon", typeof(Image), typeof(IconButton), new UIPropertyMetadata(null));

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(IconButton), new UIPropertyMetadata(""));

        public new HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }
        public static new readonly DependencyProperty HorizontalContentAlignmentProperty =
            DependencyProperty.Register("HorizontalContentAlignment", typeof(HorizontalAlignment), typeof(IconButton), new UIPropertyMetadata(HorizontalAlignment.Left));

        public IconButton()
        {
            InitializeComponent();

            this.button.DataContext = this;
            this.icon.DataContext = this;
            this.textBlock.DataContext = this;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.OnClick();
        }

        public event EventHandler<RoutedEventArgs> Click;
        private void OnClick()
        {
            if (this.Click != null)
            {
                this.Click(this, new RoutedEventArgs());
            }
        }
    }
}
