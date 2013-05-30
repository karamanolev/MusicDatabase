using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicDatabase.Engine
{
    public class CallbackEqualityComparer<T> : IEqualityComparer<T>
    {
        public Func<T, T, bool> Comparer { get; set; }

        public CallbackEqualityComparer(Func<T, T, bool> comparer)
        {
            this.Comparer = comparer;
        }

        public bool Equals(T x, T y)
        {
            return this.Comparer(x, y);
        }

        public int GetHashCode(T obj)
        {
            return 0;
        }
    }
}
