using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MusicDatabase.Engine;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ImageBox.xaml
    /// </summary>
    public partial class ImageBox : UserControl
    {
        public IList<byte> ImageBytes
        {
            get { return (IList<byte>)GetValue(ImageBytesProperty); }
            set { SetValue(ImageBytesProperty, value); }
        }
        public static readonly DependencyProperty ImageBytesProperty =
            DependencyProperty.Register("ImageBytes", typeof(IList<byte>), typeof(ImageBox), new UIPropertyMetadata(null, OnImageBytesChangedCallback));
        private static void OnImageBytesChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((ImageBox)sender).OnImageBytesChanged();
        }

        public Func<byte[]> FullImageLazy { get; set; }

        public int ImageWidth { get { return ((BitmapImage)this.image.Source).PixelWidth; } }
        public int ImageHeight { get { return ((BitmapImage)this.image.Source).PixelHeight; } }

        public double InnerWidth
        {
            get { return this.image.Width; }
            set { image.Width = value; }
        }

        public double InnerHeight
        {
            get { return this.image.Height; }
            set { this.image.Height = value; }
        }

        public bool HasImage
        {
            get { return this.ImageBytes != null; }
        }

        public ImageBox()
        {
            InitializeComponent();
        }

        private void OnImageBytesChanged()
        {
            if (this.ImageBytes != null)
            {
                BitmapImage imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = new MemoryStream((byte[])this.ImageBytes);
                imageSource.EndInit();
                this.image.Source = imageSource;

                this.labelInfo.Text = imageSource.PixelWidth + "x" + imageSource.PixelHeight + " " + Utility.BytesToString(this.ImageBytes.Count);
            }
            else
            {
                this.image.Source = null;
                this.labelInfo.Text = "No Image";
            }
            this.OnImageChanged();
        }

        private void UserControl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        public event EventHandler ImageChanged;
        private void OnImageChanged()
        {
            if (this.ImageChanged != null)
            {
                this.ImageChanged(this, EventArgs.Empty);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            byte[] bytes = null;
            if (this.FullImageLazy != null)
            {
                bytes = this.FullImageLazy();
            }
            if (bytes == null)
            {
                bytes = (byte[])this.ImageBytes;
            }
            ViewImageWindow viewImageWindow = new ViewImageWindow(bytes);
            viewImageWindow.ShowDialog();
        }
    }
}
