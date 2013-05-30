using System;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MusicDatabase.Helpers
{
    class ReleaseThumbnailConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ReleaseCoverFlow.ReleaseProxy release = value as ReleaseCoverFlow.ReleaseProxy;

            if (release == null)
            {
                return null;
            }

            if (release.Thumbnail != null)
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.StreamSource = new MemoryStream(release.Thumbnail);
                image.EndInit();
                return image;
            }
            else
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri("pack://application:,,,/Images/JewelcaseNoImage.png");
                image.EndInit();
                return image;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
