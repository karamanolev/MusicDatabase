using System;
using System.IO;
using System.Linq;
using MongoDB.Driver.GridFS;

namespace MusicDatabase.Engine.Database
{
    public static class Extensions
    {
        public static void Upload(this MongoGridFS gridFs, string remoteFilename, byte[] data)
        {
            using (Stream fileStream = gridFs.Open(remoteFilename, FileMode.Create))
            {
                fileStream.Write(data, 0, data.Length);
            }
        }

        public static byte[] DownloadBytes(this MongoGridFS gridFs, string remoteFilename)
        {
            using (Stream fileStream = gridFs.Open(remoteFilename, FileMode.Open))
            {
                byte[] buffer = new byte[fileStream.Length];
                int offset = 0;
                while (offset < buffer.Length)
                {
                    int read = fileStream.Read(buffer, offset, buffer.Length - offset);
                    if (read == 0)
                    {
                        throw new EndOfStreamException();
                    }
                    offset += read;
                }
                return buffer;
            }
        }
    }
}
