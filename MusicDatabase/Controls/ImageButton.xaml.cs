using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ImageButton.xaml
    /// </summary>
    public partial class ImageButton : UserControl
    {
        public Image Image
        {
            get { return (Image)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(Image), typeof(ImageButton), new UIPropertyMetadata(null, OnImageChangedCallback));
        private static void OnImageChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ImageButton)sender).UpdateImage();
        }

        public Image HoverImage
        {
            get { return (Image)GetValue(HoverImageProperty); }
            set { SetValue(HoverImageProperty, value); }
        }
        public static readonly DependencyProperty HoverImageProperty =
            DependencyProperty.Register("HoverImage", typeof(Image), typeof(ImageButton), new UIPropertyMetadata(null, OnHoverImageChangedCallback));
        private static void OnHoverImageChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ImageButton)sender).UpdateImage();
        }

        public ImageButton()
        {
            InitializeComponent();

            this.MouseEnter += new System.Windows.Input.MouseEventHandler(ImageButton_MouseEnter);
            this.MouseLeave += new System.Windows.Input.MouseEventHandler(ImageButton_MouseLeave);
            this.button.Click += new RoutedEventHandler(button_Click);
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            this.OnClick();
        }

        void ImageButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.UpdateImage();
        }

        void ImageButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.UpdateImage();
        }

        private void UpdateImage()
        {
            if (this.IsMouseOver && this.HoverImage != null)
            {
                this.button.Content = this.HoverImage;
            }
            else
            {
                this.button.Content = this.Image;
            }
        }

        public event EventHandler Click;
        private void OnClick()
        {
            if (this.Click != null)
            {
                this.Click(this, EventArgs.Empty);
            }
        }
    }
}
