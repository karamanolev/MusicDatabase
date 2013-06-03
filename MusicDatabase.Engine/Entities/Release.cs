using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MusicDatabase.Engine.Entities
{
    public class Release
    {
        public const string ThumbnailMimeType = "image/jpeg";
        public const int ThumbnailSize = 256;


        #region Model Properties

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string JoinedAlbumArtists { get; set; }
        public string Title { get; set; }
        public string Country { get; set; }
        public ReleaseDate ReleaseDate { get; set; }
        public ReleaseDate OriginalReleaseDate { get; set; }
        public string Genre { get; set; }
        public string Label { get; set; }
        public string CatalogNumber { get; set; }
        public bool IsFlagged { get; set; }
        public string FlagMessage { get; set; }
        public string Notes { get; set; }

        public int DiscogsReleaseId { get; set; }
        public int DiscogsMasterId { get; set; }
        public string WikipediaPageName { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DateAdded { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DateModified { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime DateAudioModified { get; set; }

        public int Score { get; set; }
        public double DynamicRange { get; set; }
        public double ReplayGainAlbumGain { get; set; }
        public double ReplayGainAlbumPeak { get; set; }

        public byte[] Thumbnail { get; set; }

        public List<Image> Images { get; set; }
        public List<Track> Tracklist { get; set; }
        public List<ReleaseAdditionalFile> AdditionalFiles { get; set; }
        public List<ReleaseArtist> Artists { get; set; }

        #endregion


        #region Aggregates

        public int DiscCount
        {
            get
            {
                return this.Tracklist.Select(t => t.Disc).Distinct().Count();
            }
        }

        public bool HasGenre
        {
            get { return !string.IsNullOrEmpty(this.Genre); }
        }

        public bool HasCatalogInformation
        {
            get { return !string.IsNullOrEmpty(this.Country) && !string.IsNullOrEmpty(this.Label) && !string.IsNullOrEmpty(this.CatalogNumber) && this.ReleaseDate.IsValid; }
        }

        public bool HasTrackArtists
        {
            get { return this.Tracklist.Any(t => !string.IsNullOrEmpty(t.JoinedArtists)); }
        }

        public ReleaseAdditionalFile TorrentFile
        {
            get { return this.AdditionalFiles.FirstOrDefault(f => f.Type == ReleaseAdditionalFileType.Torrent); }
        }

        public Image MainImage
        {
            get { return this.Images.Where(i => i.IsMain).FirstOrDefault(); }
        }

        #endregion


        public Release()
        {
            this.Artists = new List<ReleaseArtist>();
            this.JoinedAlbumArtists = "";
            this.Tracklist = new List<Track>();
            this.Images = new List<Image>();
            this.ReleaseDate = new ReleaseDate();
            this.OriginalReleaseDate = new ReleaseDate();
            this.AdditionalFiles = new List<ReleaseAdditionalFile>();
        }

        public override string ToString()
        {
            return "{" + this.Id + "} " + this.JoinedAlbumArtists + " - " + Title;
        }

        public string GetDirectory(ICollectionManager collectionManager)
        {
            return Path.GetDirectoryName(this.Tracklist[0].GetAbsoluteFilename(collectionManager));
        }

        public void UpdateDynamicProperties()
        {
            this.Score = new ReleaseScorer(this).Score;
            this.DynamicRange = this.Tracklist.Select(t => t.DynamicRange).Average();
        }
    }
}
