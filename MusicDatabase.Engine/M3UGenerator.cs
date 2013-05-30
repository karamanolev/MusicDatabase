using System.Collections.Generic;
using System.IO;
using System.Text;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public static class M3UGenerator
    {
        public static string GeneratePlaylist(Release release, IEnumerable<Track> tracklist, string musicRoot)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#EXTM3U");
            sb.AppendLine();
            foreach (Track track in tracklist)
            {
                sb.Append("#EXTINF:-1,");
                if (release.HasTrackArtists)
                {
                    sb.Append(track.JoinedArtists);
                }
                else
                {
                    sb.Append(release.JoinedAlbumArtists);
                }
                sb.AppendLine(" - "+track.Title);
                sb.AppendLine(Path.Combine(musicRoot, track.RelativeFilename));
            }
            return sb.ToString();
        }
    }
}
