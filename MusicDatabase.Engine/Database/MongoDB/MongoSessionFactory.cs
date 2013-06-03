using System;
using System.Linq;
using MongoDB.Driver;

namespace MusicDatabase.Engine.Database.MongoDB
{
    public class MongoSessionFactory : ICollectionSessionFactory
    {
        public MongoClient Client { get; private set; }
        public MongoServer Server { get; private set; }
        public MongoDatabase Database { get; private set; }
        public string DatabasePath { get; private set; }

        public MongoSessionFactory(string connectionString)
        {
            this.DatabasePath = connectionString;
            this.Client = new MongoClient(connectionString);
            this.Server = this.Client.GetServer();
            this.Database = this.Server.GetDatabase("MusicDatabase");
        }

        public void Dispose()
        {
            this.Database = null;
            this.Server = null;
            this.Client = null;
        }

        private ICollectionImageHandler CreateImageHandler()
        {
            return new GridFSImageHandler(this.Database);
        }

        public ICollectionManager CreateCollectionManager()
        {
            return new MongoCollectionManager(this.Database, this.CreateImageHandler());
        }
    }
}
