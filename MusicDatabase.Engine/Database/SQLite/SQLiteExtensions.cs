using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace MusicDatabase.Engine.Database.SQLite
{
    public static class SQLiteExtensions
    {
        public static string GetString(this SQLiteDataReader reader, string name)
        {
            return reader.GetString(reader.GetOrdinal(name));
        }

        public static long GetInt64(this SQLiteDataReader reader, string name)
        {
            return reader.GetInt64(reader.GetOrdinal(name));
        }

        public static byte[] GetBytes(this SQLiteDataReader reader, int i)
        {
            const int ChunkSize = 2 * 1024;
            byte[] buffer = new byte[ChunkSize];
            long bytesRead;
            long fieldOffset = 0;
            using (MemoryStream stream = new MemoryStream())
            {
                while ((bytesRead = reader.GetBytes(i, fieldOffset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }
                return stream.ToArray();
            }
        }

        public static byte[] GetBytes(this SQLiteDataReader reader, string name)
        {
            return reader.GetBytes(reader.GetOrdinal(name));
        }

        public static IEnumerable<string> GetTypedItems(this SQLiteConnection conn, string type)
        {
            SQLiteCommand command = conn.CreateCommand();
            command.CommandText = "SELECT type, name FROM sqlite_master";
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    if (reader.GetString("type") == type)
                    {
                        yield return reader.GetString("name");
                    }
                }
            }
        }

        public static IEnumerable<string> GetIndexNames(this SQLiteConnection conn)
        {
            return conn.GetTypedItems("index");
        }

        public static IEnumerable<string> GetTableNames(this SQLiteConnection conn)
        {
            return conn.GetTypedItems("table");
        }

        public static void ExecuteCommand(this SQLiteConnection conn, string commandText)
        {
            var create = conn.CreateCommand();
            create.CommandText = commandText;
            create.ExecuteNonQuery();
        }
    }
}
