using System;

namespace MusicDatabase.Import
{
    public class TrackAddingEventArgs : EventArgs
    {
        public ImportTrackItem ImportTrackItem { get; private set; }

        public TrackAddingEventArgs(ImportTrackItem importTrackItem)
        {
            this.ImportTrackItem = importTrackItem;
        }
    }
}
