using System;
using System.Linq;

namespace MusicDatabase.Engine
{
    public interface ITransaction : IDisposable
    {
        void Rollback();
    }
}
