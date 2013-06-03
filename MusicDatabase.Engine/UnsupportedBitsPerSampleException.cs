using System;
using System.Linq;

namespace MusicDatabase.Engine
{
    public class UnsupportedBitsPerSampleException : Exception
    {
        public UnsupportedBitsPerSampleException(string message)
            : base(message)
        {
        }
    }
}
