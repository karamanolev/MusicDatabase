using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using MusicDatabase.Engine;
using NImageMagick;
using NImageMagick.Extensions;

namespace MusicDatabase.ImageStudio
{
    public class InputImage
    {
        public const int ThumbnailSize = 1000;

        private long fileSize;
        private byte[] thumbBytes;

        public string Header
        {
            get
            {
                return this.SourceImage.Width + " x " + this.SourceImage.Height + " - " + this.SourceImage.Format + " - " + Utility.BytesToString(this.fileSize) + " @ " + this.SourceImage.Filename;
            }
        }

        public string Path { get; private set; }
        public Image SourceImage { get; private set; }
        public BitmapImage Image { get; private set; }

        public InputImage(string path)
        {
            this.fileSize = new FileInfo(path).Length;

            this.Path = path;
            this.SourceImage = new Image(path);
            Image thumbnail = new Image(this.SourceImage);
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
