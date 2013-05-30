using System.Linq;
using MusicDatabase.Engine.Entities;
using System.Collections.Generic;
using System.IO;

namespace MusicDatabase.Engine.Tagging
{
    public class AudioFileTag
    {
        public string AlbumArtists { get; set; }
        public string Artists { get; set; }
        public string Album { get; set; }
        public string Title { get; set; }
        public int Track { get; set; }
        public int TrackCount { get; set; }
        public int Disc { get; set; }
        public int DiscCount { get; set; }
        public int Year { get; set; }
        public string Genre { get; set; }
        public AudioFileImage[] Images { get; set; }

        public AudioFileTag()
        {
        }

        public AudioFileTag(Release release, Track track)
        {
            this.AlbumArtists = release.JoinedAlbumArtists;
            if (string.IsNullOrEmpty(track.JoinedArtists))
            {
                this.Artists = release.JoinedAlbumArtists;
            }
            else
            {
                this.Artists = track.JoinedArtists;
            }
            this.Album = release.Title;
            this.Title = track.Title;
            this.Track = track.Position;
            this.TrackCount = release.Tracklist.Count(t => t.Disc == track.Disc);
            this.Disc = track.Disc;
            this.DiscCount = release.DiscCount;
            if (release.OriginalReleaseDate != null && release.OriginalReleaseDate.IsValid)
            {
                this.Year = release.OriginalReleaseDate.Date.Year;
            }
            else
            {
                this.Year = release.ReleaseDate.IsValid ? release.ReleaseDate.Date.Year : 0;
            }
            this.Genre = release.Genre;

            Image mainImage = release.Images.Where(i => i.IsMain == true).FirstOrDefault();
            if (release.Thumbnail != null && mainImage != null)
            {
                this.Images = new AudioFileImage[]
                {
                    new AudioFileImage() {
                        Data = release.Thumbnail,
                        MimeType = Release.ThumbnailMimeType,
                        Description = mainImage.Description,
                        Type = mainImage.Type
                    }
                };
            }
        }

        public AudioFileTag(string source)
        {
            using (TagLib.File file = TagLib.File.Create(source))
            {

                this.AlbumArtists = file.Tag.JoinedAlbumArtists;
                this.Artists = file.Tag.JoinedPerformers;
                this.Album = file.Tag.Album;
                this.Title = file.Tag.Title;
                this.Track = (int)file.Tag.Track;
                this.TrackCount = (int)file.Tag.TrackCount;
                this.Disc = (int)file.Tag.Disc;
                this.DiscCount = (int)file.Tag.DiscCount;
                this.Year = (int)file.Tag.Year;
                this.Genre = file.Tag.JoinedGenres;

                List<AudioFileImage> images = new List<AudioFileImage>();
                foreach (TagLib.IPicture picture in file.Tag.Pictures)
                {
                    byte[] data = new byte[picture.Data.Count];
                    picture.Data.CopyTo(data, 0);
                    images.Add(new AudioFileImage()
                    {
                        Data = data,
                        MimeType = picture.MimeType,
                        Description = picture.Description,
                        Type = (ImageType)picture.Type,
                        Extension = MimeHelper.GetExtensionForMimeType(picture.MimeType)
                    });
                }
            }
        }

        public void WriteToFile(string filename)
        {
            using (TagLib.File file = TagLib.File.Create(filename))
            {
                file.Tag.AlbumArtists = new string[] { this.AlbumArtists };
                file.Tag.Performers = new string[] { this.Artists };
                file.Tag.Album = this.Album;
                file.Tag.Title = this.Title;
                file.Tag.Track = (uint)this.Track;
                file.Tag.TrackCount = (uint)this.TrackCount;
                file.Tag.Disc = (uint)this.Disc;
                file.Tag.DiscCount = (uint)this.DiscCount;
                file.Tag.Year = (uint)this.Year;
                if (this.Genre != null)
                {
                    file.Tag.Genres = new string[] { this.Genre };
                }
                if (this.Images != null)
                {
                    file.Tag.Pictures = this.Images.Select(image => new TagLib.Picture(image.Data)
                    {
                        Description = image.Description,
                        MimeType = image.MimeType,
                        Type = (TagLib.PictureType)image.Type
                    }).ToArray();
                }
                file.Save();
            }
        }

        public static void WriteReplayGainData(string filename, double trackGain, double trackPeak, double albumGain, double albumPeak, bool keepFileTimes)
        {
            FileInfo fileInfo = null;
            if (keepFileTimes)
            {
                fileInfo = new FileInfo(filename);
            }
            using (TagLib.File file = TagLib.File.Create(filename))
            {
                file.Tag.ReplayGainTrackGain = trackGain;
                file.Tag.ReplayGainTrackPeak = trackPeak;
                file.Tag.ReplayGainAlbumGain = albumGain;
                file.Tag.ReplayGainAlbumPeak = albumPeak;
                file.Save();
            }
            if (keepFileTimes)
            {
                File.SetCreationTime(filename, fileInfo.CreationTime);
                File.SetLastAccessTime(filename, fileInfo.LastAccessTime);
                File.SetLastWriteTime(filename, fileInfo.LastWriteTime);
            }
        }
    }
}
