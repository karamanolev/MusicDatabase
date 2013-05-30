using System;

namespace MusicDatabase.Audio.Mp3
{
    public class LameException : Exception
    {
        public LameException(string message)
            : base(message)
        {
        }
    }
}
