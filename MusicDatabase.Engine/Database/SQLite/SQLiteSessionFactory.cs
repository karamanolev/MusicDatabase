using System;
using System.Data.SQLite;
using System.Linq;

namespace MusicDatabase.Engine.Database.SQLite
{
    public class SQLiteSessionFactory : ICollectionSessionFactory
    {
        private string filePath;
        private SQLiteConnection connection;

        public string DatabasePath
        {
            get { return CollectionFactoryFactory.SQLitePrefix + filePath; }
        }

        public SQLiteSessionFactory(string filePath)
        {
            this.filePath = filePath;
            this.connection = new SQLiteConnection("Data Source=" + filePath + ";Version=3;DateTimeFormat=Ticks");
            this.connection.Open();
            this.CreateSchema();
        }

        private void CreateSchema()
        {
            var tableNames = connection.GetTableNames().ToArray();

            if (!tableNames.Contains("releases"))
            {
                connection.ExecuteCommand("CREATE TABLE releases (id INTEGER PRIMARY KEY, bson BLOB)");
            }

            if (!tableNames.Contains("images"))
            {
                connection.ExecuteCommand("CREATE TABLE images (id INTEGER PRIMARY KEY, image_key TEXT, data BLOB)");
                connection.ExecuteCommand("CREATE UNIQUE INDEX image_key_index ON images (image_key);");
            }

            if (!tableNames.Contains("settings"))
            {
                connection.ExecuteCommand("CREATE TABLE settings (id INTEGER PRIMARY KEY, bson BLOB)");
            }

            if (!tableNames.Contains("track_info_caches"))
            {
                connection.ExecuteCommand("CREATE TABLE track_info_caches (id INTEGER PRIMARY KEY, bson BLOB)");
            }
        }

        public ICollectionManager CreateCollectionManager()
        {
            return new SQLiteCollectionManager(this.connection, new SQLiteImageHandler(this.connection));
        }

        public void Dispose()
        {
            this.connection.Close();
            this.connection.Dispose();
        }
    }
}
