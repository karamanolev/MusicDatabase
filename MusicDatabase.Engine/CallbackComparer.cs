using System;
using System.Collections.Generic;
using System.Linq;

namespace MusicDatabase.Engine
{
    public class CallbackComparer<T> : IComparer<T>
    {
        public Func<T, T, int> Comparer { get; set; }

        public CallbackComparer(Func<T, T, int> comparer)
        {
            this.Comparer = comparer;
        }

        public int Compare(T x, T y)
        {
            return this.Comparer(x, y);
        }
    }
}
