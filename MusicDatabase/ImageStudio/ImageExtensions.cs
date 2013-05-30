using System;
using System.Collections.Generic;
using System.Linq;
using NImageMagick;

namespace MusicDatabase.ImageStudio
{
    public static class ImageExtensions
    {
        public const string DefaultFormat = "JPEG";
        public const int DefaultQuality = 85;

        public static Action<Image> SaveAction;

        public static void Save(this Image image, string format = null, int quality = -1)
        {
            if (format == null)
            {
                format = DefaultFormat;
            }
            if (quality == -1)
            {
                quality = DefaultQuality;
            }
            image.CompressionQuality = quality;
            image.Format = format;
            SaveAction(image);
        }

        public static void Save(this IEnumerable<Image> images, string format = null, int quality = -1)
        {
            foreach (Image image in images)
            {
                Save(image, format, quality);
            }
        }
    }
}
