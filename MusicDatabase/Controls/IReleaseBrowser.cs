using System;
using System.Linq;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    public interface IReleaseBrowser
    {
        CollectionManager CollectionManager { get; set; }
        string[] AlbumArtists { get; }
        bool IsEnabled { get; set; }

        int[] GetReleaseIdsByAlbumArtist(string albumArtist);
        object GetSelectedItem();
        void LoadReleases(Release[] releases);
        bool SetSelectedItem(object item);
    }
}
