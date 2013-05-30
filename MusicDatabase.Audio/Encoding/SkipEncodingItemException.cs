using System;
using System.Linq;

namespace MusicDatabase.Audio.Encoding
{
    public class SkipEncodingItemException : Exception
    {
        public SkipEncodingItemException(string message)
            : base(message)
        {
        }
    }
}
