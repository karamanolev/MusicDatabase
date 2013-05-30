using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public class FilenameGenerator
    {
        private static readonly string[] defaultPatterns = new string[] {
            @"{AlbumArtist}\{Album}{IfDiscs} CD{Disc}{/IfDiscs}\{Track}. {Title}",
            @"{AlbumArtist}\{Album}\{Track}. {Title}",
            @"{AlbumArtist}\{Album}\{Track}. {Title}",
            @"{AlbumArtist}\{Album}\{Artist} - {Track} {Title}",
            @"{Artist} - {Album}\{Track}. {Title}",
            @"{Artist}\{Album}\{Title}",
            @"{Artist} - {Title}"
        };

        private const string artistPattern = "{Artist}";
        private const string albumArtistPattern = "{AlbumArtist}";
        private const string albumPattern = "{Album}";
        private const string titlePattern = "{Title}";
        private const string trackPattern = "{Track}";
        private const string discPattern = "{Disc}";
        private const string ifDiscsStartPattern = "{IfDiscs}";
        private const string ifDiscsEndPattern = "{/IfDiscs}";

        private const string textPattern = "{Text}";
        private const string numberPattern = "{Number}";

        public static string[] DefaultPatterns { get { return defaultPatterns; } }
        public static string ArtistPattern { get { return artistPattern; } }
        public static string AlbumArtistPattern { get { return albumArtistPattern; } }
        public static string AlbumPattern { get { return albumPattern; } }
        public static string TitlePattern { get { return titlePattern; } }
        public static string TrackPattern { get { return trackPattern; } }
        public static string DiscPattern { get { return discPattern; } }
        public static string IfDiscsStartPattern { get { return ifDiscsStartPattern; } }
        public static string IfDiscsEndPattern { get { return ifDiscsEndPattern; } }
        public static string TextPattern { get { return textPattern; } }
        public static string NumberPattern { get { return numberPattern; } }

        public static string FixFilename(string name)
        {
            if (name == null)
            {
                return "";
            }

            return name
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace(":", " -")
                .Replace("|", "-")

                .Replace("*", "")
                .Replace("<", "")
                .Replace(">", "")
                .Replace("?", "")
                .Replace("\"", "");
        }

        public static string PatternToFilename(string pattern, Release release, Track track)
        {
            if (release.DiscCount <= 1)
            {
                while (true)
                {
                    int start = pattern.IndexOf(IfDiscsStartPattern);
                    if (start == -1)
                    {
                        break;
                    }

                    int end = pattern.IndexOf(IfDiscsEndPattern, start);
                    if (end == -1)
                    {
                        break;
                    }

                    pattern = pattern.Substring(0, start) + pattern.Substring(end + IfDiscsEndPattern.Length);
                }

            }

            string actualTrackArtists = string.IsNullOrEmpty(track.JoinedArtists) ? release.JoinedAlbumArtists : track.JoinedArtists;

            return pattern
                .Replace(ArtistPattern, actualTrackArtists)
                .Replace(AlbumArtistPattern, FixFilename(release.JoinedAlbumArtists))
                .Replace(AlbumPattern, FixFilename(release.Title))
                .Replace(TitlePattern, FixFilename(track.Title))
                .Replace(TrackPattern, FixFilename(track.Position.ToString("00")))
                .Replace(DiscPattern, FixFilename(track.Disc.ToString()))

                .Replace(TextPattern, "")
                .Replace(NumberPattern, "")
                .Replace(IfDiscsStartPattern, "")
                .Replace(IfDiscsEndPattern, "")
                ;
        }
    }
}
