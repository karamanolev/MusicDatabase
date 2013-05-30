using System;
using System.Linq;
using NHibernate;

namespace MusicDatabase.Engine
{
    public interface ICollectionSessionFactory : IDisposable
    {
        string DatabasePath { get; }
        ISession CreateSession();
        ICollectionImageHandler CreateImageHandler();
    }
}
