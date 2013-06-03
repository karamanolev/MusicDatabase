using System;
using System.Linq;
using MusicDatabase.Engine;

namespace MusicDatabase.Helpers
{
    class HelperBase : IDisposable
    {
        public MusicDatabaseWindow ParentWindow { get; private set; }
        public ICollectionManager CollectionManager { get; private set; }

        public HelperBase(MusicDatabaseWindow parentWindow, ICollectionSessionFactory collectionSessionFactory)
        {
            this.ParentWindow = parentWindow;
            this.CollectionManager = collectionSessionFactory.CreateCollectionManager();
        }

        public void Dispose()
        {
            this.CollectionManager.Dispose();
            this.CollectionManager = null;
        }
    }
}
