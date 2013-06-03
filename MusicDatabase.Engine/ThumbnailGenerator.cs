using System;
using System.Linq;
using MusicDatabase.Engine.Entities;
using NImageMagick;
using Image = MusicDatabase.Engine.Entities.Image;

namespace MusicDatabase.Engine
{
    public class ThumbnailGenerator
    {
        public static void UpdateReleaseThumbnail(Release release, ICollectionImageHandler imageHandler)
        {
            Image mainImage = release.Images.FirstOrDefault(i => i.IsMain);
            if (mainImage != null)
            {
                byte[] thumbnail = null;
                string thumbExtension = null, thumbMimeType = null;
                thumbnail = ThumbnailGenerator.CreateThumbnail(
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
    }
}
