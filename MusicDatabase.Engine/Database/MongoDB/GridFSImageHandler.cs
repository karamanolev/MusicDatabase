using System;
using System.IO;
using System.Linq;
using System.Xml;
using MongoDB.Driver;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine.Database.MongoDB
{
    public class GridFSImageHandler : ICollectionImageHandler
    {
        public MongoDatabase Database { get; private set; }

        public GridFSImageHandler(MongoDatabase database)
        {
            this.Database = database;
        }

        public long GetImageByteLength(Image image)
        {
            var fileInfo = this.Database.GridFS.FindOne(GetImageFilename(image));
            return fileInfo.Length;
        }

        public byte[] LoadImage(Image image)
        {
            return this.Database.GridFS.DownloadBytes(GetImageFilename(image));
        }

        public void StoreImage(Image image, byte[] bytes)
        {
            this.DeleteImage(image);
            this.Database.GridFS.Upload(GetImageFilename(image), bytes);
        }

        public void StoreImageFromXml(Image image, XmlReader reader)
        {
            string remoteFilename = GetImageFilename(image);
            using (Stream fileStream = this.Database.GridFS.Open(remoteFilename, FileMode.Create))
            {
                byte[] readBuffer = new byte[128 * 1024];
                int read;
                while ((read = reader.ReadElementContentAsBase64(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    fileStream.Write(readBuffer, 0, read);
                }
            }
        }

        public void DeleteImage(Image image)
        {
            this.Database.GridFS.Delete(GetImageFilename(image));
        }

        public static string GetImageFilename(Image image)
        {
            return "images/" + image.Id.ToString();
        }
    }
}
