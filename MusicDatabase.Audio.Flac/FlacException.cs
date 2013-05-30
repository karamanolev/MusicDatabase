using System;

namespace MusicDatabase.Audio.Flac
{
    class FlacException : Exception
    {
        public FlacException(string message)
            : base(message)
        {
        }
    }
}
