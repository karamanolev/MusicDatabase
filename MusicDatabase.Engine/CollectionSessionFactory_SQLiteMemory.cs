using System;
using System.Linq;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Fluent;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace MusicDatabase.Engine
{
    public class CollectionSessionFactory_SQLiteMemory : ICollectionSessionFactory
    {
        private Configuration configuration;
        private ISessionFactory sessionFactory;

        public string DatabasePath
        {
            get { return ":memory:"; }
        }

        public CollectionSessionFactory_SQLiteMemory()
        {
            this.configuration = FluentHelper.CreateSessionFactory_SQLiteInMemory(typeof(Release).Assembly);
            this.sessionFactory = this.configuration.BuildSessionFactory();
        }

        public ISession CreateSession()
        {
            var session = this.sessionFactory.OpenSession();
            new SchemaExport(this.configuration).Execute(false, true, false, session.Connection, null);
            return session;
        }

        public ICollectionImageHandler CreateImageHandler()
        {
            return new MemoryImageHandler();
        }

        public void Dispose()
        {
            this.sessionFactory.Dispose();
        }
    }
}
