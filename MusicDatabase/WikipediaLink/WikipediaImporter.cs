using System;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.WikipediaLink
{
    public class WikipediaImporter
    {
        private MusicDatabaseWindow parent;
        private Release release;

        public WikipediaImporter(MusicDatabaseWindow parent, Release release)
        {
            this.parent = parent;
            this.release = release;
        }

        public bool Import()
        {
            WikipediaImporterWindow window = new WikipediaImporterWindow(this.release);
            if (window.ShowDialog(parent) == true)
            {
                this.release.WikipediaPageName = window.WikipediaPageName;
                if (window.ImportOriginalReleaseDate)
                {
                    this.release.OriginalReleaseDate = window.OriginalReleaseDate;
                    if (this.ShouldWarn())
                    {
                        Dialogs.Warn("The current release date is before the original release date! Please check!");
                    }
                }
                return true;
            }
            return false;
        }

        public bool ShouldWarn()
        {
            if (this.release.OriginalReleaseDate.Type >= ReleaseDateType.Year && this.release.ReleaseDate.Type >= ReleaseDateType.Year)
            {
                if (this.release.OriginalReleaseDate.Date.Year > this.release.ReleaseDate.Date.Year)
                {
                    return true;
                }
            }
            if (this.release.OriginalReleaseDate.Type >= ReleaseDateType.YearMonth && this.release.ReleaseDate.Type >= ReleaseDateType.YearMonth)
            {
                if (this.release.OriginalReleaseDate.Date.Year == this.release.ReleaseDate.Date.Year &&
                    this.release.OriginalReleaseDate.Date.Month > this.release.ReleaseDate.Date.Month)
                {
                    return true;
                }
            }
            if (this.release.OriginalReleaseDate.Type >= ReleaseDateType.YearMonthDay && this.release.ReleaseDate.Type >= ReleaseDateType.YearMonthDay)
            {
                if (this.release.OriginalReleaseDate.Date.Year == this.release.ReleaseDate.Date.Year &&
                    this.release.OriginalReleaseDate.Date.Month == this.release.ReleaseDate.Date.Month &&
                    this.release.OriginalReleaseDate.Date.Day > this.release.ReleaseDate.Date.Day)
                {
                    return true;
                }
            }
            return false;
        }

        public static string MakePageName(string name)
        {
            return name.Replace(' ', '_');
        }

        public static string MakeSearchUrlFromPageName(string pageName)
        {
            return "http://en.wikipedia.org/wiki/search-redirect.php?search=" + Uri.EscapeDataString(pageName);
        }

        public static string MakeUrlFromPageName(string pageName)
        {
            return "http://en.wikipedia.org/wiki/" + MakePageName(pageName);
        }
    }
}
