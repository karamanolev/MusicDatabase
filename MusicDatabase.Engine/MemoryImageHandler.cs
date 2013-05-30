using System;
using System.IO;
using System.Linq;
using System.Xml;
using MusicDatabase.Engine.Entities;
using System.Collections.Generic;

namespace MusicDatabase.Engine
{
    public class MemoryImageHandler : ICollectionImageHandler
    {
        private Dictionary<Image, byte[]> data = new Dictionary<Image, byte[]>();

        public MemoryImageHandler()
        {
            this.data = new Dictionary<Image, byte[]>();
        }

        public byte[] LoadImage(Image image)
        {
            return this.data[image];
        }

        public void StoreImage(Image image, byte[] bytes)
        {
            this.data[image] = bytes;
        }

        public void StoreImageFromXml(Image image, XmlReader reader)
        {
            MemoryStream memoryStream = new MemoryStream();

            byte[] readBuffer = new byte[128 * 1024];
            int read;
            while ((read = reader.ReadElementContentAsBase64(readBuffer, 0, readBuffer.Length)) > 0)
            {
                memoryStream.Write(readBuffer, 0, read);
            }

            this.data[image] = memoryStream.ToArray();
        }

        public void Clear()
        {
            this.data.Clear();
        }

        public void DeleteImage(Image image)
        {
            this.data.Remove(image);
        }

        public long GetImageByteLength(Image image)
        {
            return data[image].Length;
        }
    }
}
