using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MusicDatabase.Import
{
    public class ImportReleaseItem
    {
        public List<ObservableCollection<ImportTrackItem>> Discs { get; set; }

        public ImportReleaseItem()
        {
            this.Discs = new List<ObservableCollection<ImportTrackItem>>();
        }
    }
}
