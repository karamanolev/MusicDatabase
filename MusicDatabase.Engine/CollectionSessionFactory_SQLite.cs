using System;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Fluent;
using NHibernate;
using System.IO;

namespace MusicDatabase.Engine
{
    public class CollectionSessionFactory_SQLite : ICollectionSessionFactory
    {
        private string databaseFilePath;
        private ISessionFactory sessionFactory;

        private string DatabaseRoot
        {
            get
            {
                return Path.GetDirectoryName(this.databaseFilePath);
            }
        }

        public string DatabasePath
        {
            get { return CollectionFactoryFactory.SQLitePrefix + this.databaseFilePath; }
        }

        public CollectionSessionFactory_SQLite(string databasePath)
        {
            this.databaseFilePath = databasePath;
            this.sessionFactory = FluentHelper.CreateSessionFactory_SQLite(typeof(Release).Assembly, this.databaseFilePath);
        }

        public ISession CreateSession()
        {
            return this.sessionFactory.OpenSession();
        }

        public ICollectionImageHandler CreateImageHandler()
        {
            return new FileImageHandler(this.DatabaseRoot);
        }

        public void Dispose()
        {
            this.sessionFactory.Dispose();
        }
    }
}
