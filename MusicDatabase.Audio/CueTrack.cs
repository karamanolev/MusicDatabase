using System;
using System.Collections.Generic;

namespace MusicDatabase.Audio
{
    public class CueTrack
    {
        public string Title { get; set; }
        public string Performer { get; set; }
        public Dictionary<int, long> Indexes { get; private set; }

        public CueTrack()
        {
            this.Indexes = new Dictionary<int, long>();
        }

        internal long GetIndex(params int[] indexPriority)
        {
            foreach (var i in indexPriority)
            {
                if (Indexes.ContainsKey(i))
                {
                    return Indexes[i];
                }
            }
            throw new InvalidOperationException();
        }
    }
}
