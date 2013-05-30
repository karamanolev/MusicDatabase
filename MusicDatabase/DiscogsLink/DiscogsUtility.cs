using System.Text.RegularExpressions;

namespace MusicDatabase.DiscogsLink
{
    public static class DiscogsUtility
    {
        public const string StandardVariousArtistsName = "Various Artists";

        public static bool IsVariousArtists(string text)
        {
            switch (text.Trim().ToLower())
            {
                case "various":
                case "various artist":
                case "various artists":
                    return true;
                default:
                    return false;
            }
        }

        public static int GetReleaseId(string text)
        {
            int releaseId;

            if (int.TryParse(text, out releaseId))
            {
                return releaseId;
            }

            Regex urlRegex = new Regex("(http://)?(www\\.)?discogs\\.com/([^/]+/)?release/(?<ReleaseId>[0-9]+)");
            Match match = urlRegex.Match(text);
            if (match.Success)
            {
                if (int.TryParse(match.Groups["ReleaseId"].Value, out releaseId))
                {
                    return releaseId;
                }
            }

            return 0;
        }
    }
}
