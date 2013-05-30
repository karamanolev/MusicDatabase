using System;
using System.Linq;

namespace MusicDatabase.Engine
{
    public class CollectionFactoryFactory
    {
        public static readonly string SQLitePrefix = "sqlite://";
        public static readonly string MySQLPrefix = "mysql://";

        public static ICollectionSessionFactory CreateFactory(string databasePath)
        {
            if (databasePath.StartsWith(SQLitePrefix))
            {
                databasePath = databasePath.Substring(SQLitePrefix.Length);
                return new CollectionSessionFactory_SQLite(databasePath);
            }
            else if (databasePath.StartsWith(MySQLPrefix))
            {
                databasePath = databasePath.Substring(MySQLPrefix.Length);
                return new CollectionSessionFactory_MySQL(databasePath);
            }

            throw new NotSupportedException("Unsupported database path: " + databasePath);
        }
    }
}
