using System;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MusicDatabase
{
    public class IconExtension : MarkupExtension
    {
        public string Path { get; set; }

        public IconExtension(string path)
        {
            this.Path = path;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri("pack://application:,,,/Images/" + this.Path + ".png");
                image.EndInit();
                return new Image()
                {
                    Source = image,
                    Stretch = Stretch.Fill,
                    Width = image.PixelWidth,
                    Height = image.PixelHeight
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
