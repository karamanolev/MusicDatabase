using System;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.Fluent;
using NHibernate;

namespace MusicDatabase.Engine
{
    public class CollectionSessionFactory_MySQL : ICollectionSessionFactory
    {
        class ImageHandler_MySQL : ICollectionImageHandler
        {
            public byte[] LoadImage(Image image)
            {
                return null;
            }

            public void StoreImage(Image image, byte[] bytes)
            {
            }

            public void StoreImageFromXml(Image image, System.Xml.XmlReader reader)
            {
            }

            public void DeleteImage(Image image)
            {
            }

            public long GetImageByteLength(Image image)
            {
                return 0;
            }
        }

        public static string MakeConnectionString(string host, string user, string pass, string db)
        {
            return "host=" + host + ";username=" + user + ";password=" + pass + ";database=" + db;
        }

        private ISessionFactory sessionFactory;

        public string DatabasePath { get; private set; }

        public CollectionSessionFactory_MySQL(string connectionString)
        {
            this.DatabasePath = CollectionFactoryFactory.MySQLPrefix + connectionString;
            this.sessionFactory = FluentHelper.CreateSessionFactory_MySQL(typeof(Release).Assembly, connectionString);
        }

        public ISession CreateSession()
        {
            return this.sessionFactory.OpenSession();
        }

        public ICollectionImageHandler CreateImageHandler()
        {
            return new ImageHandler_MySQL();
        }

        public void Dispose()
        {
            this.sessionFactory.Dispose();
        }
    }
}
