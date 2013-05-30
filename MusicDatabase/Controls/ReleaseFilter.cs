using System;
using System.Collections.Generic;
using System.Linq;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    class ReleaseFilter
    {
        private static readonly char[] SearchStringSplitChars = new char[] { ' ', '\t', ',' };

        private string searchString;
        private string[] searchParts;

        public string SearchString
        {
            get { return this.searchString; }
            set
            {
                this.searchString = value;
                this.searchParts = value.ToLower().Split(SearchStringSplitChars, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public bool AdditionalFiltering { get; set; }
        public bool? HasImages { get; set; }
        public bool? HasFiles { get; set; }
        public bool? IsFlagged { get; set; }
        public bool? HasWikiPage { get; set; }
        public bool DoExtendedSearch { get; set; }

        public bool Match(Release release)
        {
            if (this.searchParts.Length > 0)
            {
                List<string> releaseSearchParts = new List<string>();

                releaseSearchParts.Add(Utility.RemoveDiacritics(release.Title.ToLower()));
                releaseSearchParts.Add(Utility.RemoveDiacritics(release.JoinedAlbumArtists.ToLower()));

                if (this.DoExtendedSearch)
                {
                    foreach (Track track in release.Tracklist)
                    {
                        releaseSearchParts.Add(Utility.RemoveDiacritics(track.Title.ToLower()));
                        if (!string.IsNullOrEmpty(track.JoinedArtists))
                        {
                            releaseSearchParts.Add(Utility.RemoveDiacritics(track.JoinedArtists.ToLower()));
                        }
                    }
                }

                foreach (string word in this.searchParts)
                {
                    bool success = false;

                    foreach (string releaseSearchPart in releaseSearchParts)
                    {
                        if (releaseSearchPart.Contains(word))
                        {
                            success = true;
                            break;
                        }
                    }

                    if (!success)
                    {
                        return false;
                    }
                }
            }

            if (this.AdditionalFiltering)
            {
                bool pass = true;

                if (pass && this.HasImages != null)
                {
                    bool hasImages = release.Images.Count != 0;
                    pass &= this.HasImages == hasImages;
                }
                if (pass && this.HasFiles != null)
                {
                    bool hasFiles = release.AdditionalFiles.Count != 0;
                    pass &= this.HasFiles == hasFiles;
                }
                if (pass && this.IsFlagged != null)
                {
                    bool isFlagged = release.IsFlagged;
                    pass &= this.IsFlagged == isFlagged;
                }
                if (pass && this.HasWikiPage != null)
                {
                    bool hasWikiPage = !string.IsNullOrEmpty(release.WikipediaPageName);
                    pass &= this.HasWikiPage == hasWikiPage;
                }

                if (!pass)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
