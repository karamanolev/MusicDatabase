using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Import
{
    public class ImportTrackItem
    {
        public Track Track { get; private set; }
        public IImportSourceItem SourceItem { get; private set; }

        public ImportTrackItem(Track track, IImportSourceItem sourceItem)
        {
            this.Track = track;
            this.SourceItem = sourceItem;
        }
    }
}
