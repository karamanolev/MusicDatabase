using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using MusicDatabase.Engine;
using NImageMagick;
using NImageMagick.Extensions;

namespace MusicDatabase.ImageStudio
{
    public class OutputImage
    {
        public const int ThumbnailSize = 1000;

        private byte[] thumbBytes;
        private string format;
        private int width, height;

        public string Header
        {
            get
            {
                return this.width + " x " + this.height + " - " + this.format + " - " + Utility.BytesToString(this.Bytes.Length);
            }
        }

        public BitmapImage Image { get; private set; }
        public byte[] Bytes { get; private set; }
        public string Extension
        {
            get { return "." + this.format.ToLower(); }
        }

        public OutputImage(Image sourceImage)
        {
            this.width = sourceImage.Width;
            this.height = sourceImage.Height;
            this.format = sourceImage.Format;
            this.Bytes = sourceImage.GetBlob();

            Image thumbnail = new Image(sourceImage);
            thumbnail.Fit(ThumbnailSize, ThumbnailSize);
            thumbnail.Format = "JPG";
            this.thumbBytes = thumbnail.GetBlob();
        }

        public void InitSource()
        {
            this.Image = new BitmapImage();
            this.Image.BeginInit();
            this.Image.StreamSource = new MemoryStream(this.thumbBytes);
            this.Image.EndInit();

            this.thumbBytes = null;
        }
    }
}
