using System;
using System.Linq;

namespace MusicDatabase.Engine
{
    interface ITransactionalCollectionManager : ICollectionManager
    {
        ITransaction BeginTransaction();
    }
}
