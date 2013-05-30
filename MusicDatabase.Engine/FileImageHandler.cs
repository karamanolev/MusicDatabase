using System;
using System.IO;
using System.Linq;
using System.Xml;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    class FileImageHandler : ICollectionImageHandler
    {
        private string databaseRoot;

        public FileImageHandler(string databaseRoot)
        {
            this.databaseRoot = databaseRoot;
        }

        private string GetFilename(Image image)
        {
            return Path.Combine(this.databaseRoot, "Images", image.Id + image.Extension);
        }

        public byte[] LoadImage(Image image)
        {
            return File.ReadAllBytes(this.GetFilename(image));
        }

        public void StoreImage(Image image, byte[] bytes)
        {
            string filename = this.GetFilename(image);
            string directory = Path.GetDirectoryName(filename);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            File.WriteAllBytes(this.GetFilename(image), bytes);
        }

        public void StoreImageFromXml(Image image, XmlReader reader)
        {
            string filename = this.GetFilename(image);
            string directory = Path.GetDirectoryName(filename);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream file = File.Create(filename))
            {
                byte[] readBuffer = new byte[128 * 1024];
                int read;
                while ((read = reader.ReadElementContentAsBase64(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    file.Write(readBuffer, 0, read);
                }
            }
        }


        public void DeleteImage(Image image)
        {
            File.Delete(this.GetFilename(image));
        }

        public long GetImageByteLength(Image image)
        {
            return new FileInfo(this.GetFilename(image)).Length;
        }
    }
}
