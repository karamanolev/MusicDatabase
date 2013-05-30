using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MusicDatabase
{
    public static class ExtensionMethods
    {
        public static void InvokeAction(this Dispatcher dispatcher, Action action)
        {
            dispatcher.Invoke(action);
        }

        public static void BeginInvokeAction(this Dispatcher dispatcher, Action action)
        {
            dispatcher.BeginInvoke(action);
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T item)
        {
            int i = 0;
            foreach (T sourceItem in source)
            {
                if (sourceItem.Equals(item))
                {
                    return i;
                }
                ++i;
            }
            return -1;
        }
    }
}
