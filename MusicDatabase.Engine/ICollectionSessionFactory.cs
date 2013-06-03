using System;
using System.Linq;

namespace MusicDatabase.Engine
{
    public interface ICollectionSessionFactory : IDisposable
    {
        string DatabasePath { get; }
        ICollectionManager CreateCollectionManager();
    }
}
