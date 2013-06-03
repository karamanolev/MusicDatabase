using System;
using System.Linq;
using MusicDatabase.Engine.Database.MongoDB;
using MusicDatabase.Engine.Database.SQLite;

namespace MusicDatabase.Engine
{
    public class CollectionFactoryFactory
    {
        public static readonly string SQLitePrefix = "sqlite://";
        public static readonly string MySQLPrefix = "mysql://";
        public static readonly string MongoDBPrefix = "mongodb://";

        public static ICollectionSessionFactory CreateFactory(string databasePath)
        {
            if (databasePath.StartsWith(SQLitePrefix))
            {
                databasePath = databasePath.Substring(SQLitePrefix.Length);
                return new SQLiteSessionFactory(databasePath);
            }
            else if (databasePath.StartsWith(MySQLPrefix))
            {
                //databasePath = databasePath.Substring(MySQLPrefix.Length);
            }
            else if (databasePath.StartsWith(MongoDBPrefix))
            {
                return new MongoSessionFactory(databasePath);
            }
            throw new NotSupportedException("Unsupported database path: " + databasePath);
        }
    }
}
