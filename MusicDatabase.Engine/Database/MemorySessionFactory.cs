using System;
using System.Linq;

namespace MusicDatabase.Engine.Database
{
    public class MemorySessionFactory : ICollectionSessionFactory
    {
        private MemoryImageHandler imageHandler;

        public MemorySessionFactory()
        {
            this.imageHandler = new MemoryImageHandler();
        }

        public string DatabasePath
        {
            get { return "memory://"; }
        }

        public ICollectionManager CreateCollectionManager()
        {
            return new MemoryCollectionManager(imageHandler);
        }

        public void Dispose()
        {
            this.imageHandler = null;
        }
    }
}
