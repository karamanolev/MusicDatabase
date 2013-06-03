using System;
using System.Linq;

namespace MusicDatabase.Engine
{
    public static class CollectionManagerGlobal
    {
        public static event EventHandler CollectionChanged;
        public static void OnCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(null, EventArgs.Empty);
            }
        }
    }
}
