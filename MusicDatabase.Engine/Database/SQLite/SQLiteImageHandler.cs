using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using MusicDatabase.Engine.Entities;
using System.Xml;
using System.IO;

namespace MusicDatabase.Engine.Database.SQLite
{
    class SQLiteImageHandler : ICollectionImageHandler
    {
        private SQLiteConnection connection;

        private SQLiteCommand getImageLengthCommand;
        private SQLiteCommand getImageCommand;
        private SQLiteCommand insertImage;
        private SQLiteCommand deleteImage;

        public SQLiteImageHandler(SQLiteConnection connection)
        {
            this.connection = connection;

            this.getImageLengthCommand = this.connection.CreateCommand();
            this.getImageLengthCommand.CommandText = "SELECT LENGTH(data) FROM images WHERE image_key = @image_key";
            this.getImageLengthCommand.Parameters.Add("image_key", DbType.String);

            this.getImageCommand = this.connection.CreateCommand();
            this.getImageCommand.CommandText = "SELECT data FROM images WHERE image_key = @image_key";
            this.getImageCommand.Parameters.Add("image_key", DbType.String);

            this.insertImage = this.connection.CreateCommand();
            this.insertImage.CommandText = "INSERT INTO images (image_key, data) VALUES (@image_key, @data)";
            this.insertImage.Parameters.Add("image_key", DbType.String);
            this.insertImage.Parameters.Add("data", DbType.Binary);

            this.deleteImage = this.connection.CreateCommand();
            this.deleteImage.CommandText = "DELETE FROM images WHERE image_key = @image_key";
            this.deleteImage.Parameters.Add("image_key", DbType.String);
        }

        public long GetImageByteLength(Image image)
        {
            this.getImageLengthCommand.Parameters["image_key"].Value = image.Id;
            using (var reader = this.getImageLengthCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    return reader.GetInt64(0);
                }
            }
            throw new KeyNotFoundException("No image with that key exists.");
        }

        public byte[] LoadImage(Image image)
        {
            this.getImageCommand.Parameters["image_key"].Value = image.Id;
            using (var reader = this.getImageCommand.ExecuteReader())
            {
                while (reader.Read())
                {
                    return reader.GetBytes("data");
                }
            }
            throw new KeyNotFoundException("No image with that key exists.");
        }

        public void StoreImage(Image image, byte[] bytes)
        {
            this.insertImage.Parameters["image_key"].Value = image.Id;
            this.insertImage.Parameters["data"].Value = bytes;
            this.insertImage.ExecuteNonQuery();
        }

        public void StoreImageFromXml(Image image, XmlReader reader)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                byte[] readBuffer = new byte[128 * 1024];
                int read;
                while ((read = reader.ReadElementContentAsBase64(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    memoryStream.Write(readBuffer, 0, read);
                }
                this.StoreImage(image, memoryStream.ToArray());
            }
        }

        public void DeleteImage(Image image)
        {
            this.deleteImage.Parameters["image_key"].Value = image.Id;
            this.deleteImage.ExecuteNonQuery();
        }
    }
}
