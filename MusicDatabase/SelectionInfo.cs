using System;
using System.Linq;

namespace MusicDatabase
{
    public class SelectionInfo
    {
        public SelectionInfoType Type { get; set; }
        public string ReleaseId { get; set; }
        public string ArtistName { get; set; }

        public SelectionInfo(SelectionInfoType type)
        {
            if (type != SelectionInfoType.None)
            {
                throw new ArgumentException();
            }
            this.Type = type;
        }

        public SelectionInfo(SelectionInfoType type, string id)
        {
            if (type == SelectionInfoType.None)
            {
                throw new ArgumentException();
            }
            else if (type == SelectionInfoType.Artist)
            {
                this.Type = type;
                this.ArtistName = id;
            }
            else if (type == SelectionInfoType.Release)
            {
                this.Type = type;
                this.ReleaseId = id;
            }
        }

        public static SelectionInfo Artist(string artistName)
        {
            return new SelectionInfo(SelectionInfoType.Artist, artistName);
        }

        public static SelectionInfo Release(string releaseId)
        {
            return new SelectionInfo(SelectionInfoType.Release, releaseId);
        }
    }
}
