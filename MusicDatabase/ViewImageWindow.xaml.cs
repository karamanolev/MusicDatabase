using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ViewImageWindow.xaml
    /// </summary>
    public partial class ViewImageWindow : Window
    {
        private BitmapImage bitmapImage;

        public ViewImageWindow(byte[] imageData)
        {
            InitializeComponent();

            this.labelInfo.Visibility = Visibility.Collapsed;

            this.Dispatcher.BeginInvokeAction(() =>
            {
                this.bitmapImage = new BitmapImage();
                this.bitmapImage.BeginInit();
                this.bitmapImage.StreamSource = new MemoryStream(imageData);
                this.bitmapImage.EndInit();
                this.image.Source = this.bitmapImage;

                double availableWidth = this.ActualWidth - 40;
                double availableHeight = this.ActualHeight - 40;

                if (availableWidth > this.bitmapImage.PixelWidth && availableHeight > this.bitmapImage.PixelHeight)
                {
                    this.image.Width = this.bitmapImage.PixelWidth;
                    this.image.Height = this.bitmapImage.PixelHeight;
                }
                else
                {
                    double ratio = Math.Min(availableWidth / this.bitmapImage.PixelWidth, availableHeight / this.bitmapImage.PixelHeight);
                    this.image.Width = ratio * this.bitmapImage.PixelWidth;
                    this.image.Height = ratio * this.bitmapImage.PixelHeight;
                }

                this.MouseMove += (_sender, _e) => this.UpdateLabelInfo(_e);
            });
        }

        private void UpdateLabelInfo(MouseEventArgs e)
        {
            this.labelInfo.Visibility = this.InputHitTest(e.GetPosition(this)) == this.image ? Visibility.Visible : Visibility.Hidden;
            Point p = e.GetPosition(this.image);
            double scale = this.bitmapImage.PixelWidth / this.image.Width;
            int actualX = (int)(scale * p.X);
            int actualY = (int)(scale * p.Y);
            this.labelInfo.Text = actualX + " x " + actualY;
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
