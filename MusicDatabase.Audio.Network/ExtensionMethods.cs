using System;
using System.IO;

namespace MusicDatabase.Audio.Network
{
    static class ExtensionMethods
    {
        public static void Write(this Stream stream, byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this Stream stream, int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes);
        }

        public static void Write(this Stream stream, long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            stream.Write(bytes);
        }

        public static void Write(this Stream stream, string value)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            stream.Write(bytes.Length);
            stream.Write(bytes);
        }

        public static byte[] ReadBytes(this Stream stream, int length)
        {
            byte[] buffer = new byte[length];
            stream.ReadBytes(buffer, 0, buffer.Length);
            return buffer;
        }

        public static void ReadBytes(this Stream stream, byte[] buffer, int offset, int length)
        {
            int read;
            while (length > 0)
            {
                read = stream.Read(buffer, offset, length);
                if (read == 0)
                {
                    throw new EndOfStreamException();
                }
                offset += read;
                length -= read;
            }
        }

        public static int ReadInt32(this Stream stream)
        {
            return BitConverter.ToInt32(stream.ReadBytes(sizeof(int)), 0);
        }

        public static long ReadInt64(this Stream stream)
        {
            return BitConverter.ToInt64(stream.ReadBytes(sizeof(long)), 0);
        }

        public static string ReadString(this Stream stream)
        {
            int length = stream.ReadInt32();
            return System.Text.Encoding.UTF8.GetString(stream.ReadBytes(length));
        }
    }
}
