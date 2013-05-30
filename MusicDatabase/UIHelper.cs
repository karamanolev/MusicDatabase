using System;
using System.Linq;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using NImageMagick;
using Image = MusicDatabase.Engine.Entities.Image;
using MagickImage = NImageMagick.Image;
using System.Windows.Media;

namespace MusicDatabase
{
    public static class UIHelper
    {
        public static void UpdateReleaseThumbnail(Release release, ICollectionImageHandler imageHandler)
        {
            Image mainImage = release.Images.FirstOrDefault(i => i.IsMain);
            if (mainImage != null)
            {
                byte[] thumbnail = null;
                string thumbExtension = null, thumbMimeType = null;
                thumbnail = UIHelper.CreateThumbnail(
                              imageHandler.LoadImage(mainImage),
                              MusicDatabase.Engine.Entities.Release.ThumbnailSize,
                              out thumbExtension,
                              out thumbMimeType);
                release.Thumbnail = thumbnail;
                Assert.IsTrue(thumbMimeType == MusicDatabase.Engine.Entities.Release.ThumbnailMimeType);
            }
            else
            {
                release.Thumbnail = null;
            }
        }

        public static byte[] CreateThumbnail(byte[] source, int size, out string extension, out string mimeType)
        {
            MagickImage image = new MagickImage(source);
            int newSize = Math.Min(image.Width, image.Height);
            int x = (image.Width - newSize) / 2;
            int y = (image.Height - newSize) / 2;
            image.Crop(newSize, newSize, x, y);
            image.Resize(size, size, FilterType.CubicFilter, 1);

            extension = ".jpeg";
            mimeType = "image/jpeg";

            image.Format = "JPEG";
            return image.GetBlob();
        }

        private static byte Blend(byte a, byte b, double coef)
        {
            return (byte)(a * coef + b * (1 - coef));
        }

        public static Color Blend(Color a, Color b, double coef)
        {
            return Color.FromArgb(
                Blend(a.A, b.A, coef),
                Blend(a.R, b.R, coef),
                Blend(a.G, b.G, coef),
                Blend(a.B, b.B, coef)
                );
        }

        internal static SolidColorBrush GetDrBrush(int dr, double alpha = 1)
        {
            double coef = Utility.Clamp((dr - 7) / 7.0, 0, 1);
            byte alphaByte = (byte)(alpha * 255);
            Color drGreen = Color.FromArgb(alphaByte, 0, 0xC4, 0);
            Color drRed = Color.FromArgb(alphaByte, 0xC4, 0, 0);
            return new SolidColorBrush(UIHelper.Blend(drGreen, drRed, coef));
        }
    }
}
