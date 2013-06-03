using System;
using System.Linq;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    public interface IReleaseBrowser
    {
        ICollectionManager CollectionManager { get; set; }
        string[] AlbumArtists { get; }
        bool IsEnabled { get; set; }

        string[] GetReleaseIdsByAlbumArtist(string albumArtist);
        SelectionInfo GetSelectedItem();
        void LoadReleases(Release[] releases);
        bool SetSelectedItem(SelectionInfo info);
    }
}
