using System.Collections.Generic;
using System.Linq;
using FluentNHibernate.Mapping;
using System.IO;
using System;

namespace MusicDatabase.Engine.Entities
{
    public class Release
    {
        public class ReleaseMap : ClassMap<Release>
        {
            public ReleaseMap()
            {
                Id(x => x.Id);
                HasMany(x => x.Artists).AsList(x => x.Column("Pos")).Cascade.AllDeleteOrphan().LazyLoad();
                Map(x => x.JoinedAlbumArtists);
                Map(x => x.Title);
                Map(x => x.Country);
                Component(x => x.ReleaseDate).ColumnPrefix("ReleaseDate_");
                Component(x => x.OriginalReleaseDate).ColumnPrefix("OriginalReleaseDate_");
                Map(x => x.DiscCount);
                Map(x => x.Genre);
                HasMany(x => x.Tracklist).AsList(x => x.Column("Pos")).Cascade.AllDeleteOrphan().LazyLoad();
                Map(x => x.Label);
                Map(x => x.CatalogNumber);
                Map(x => x.Thumbnail).LazyLoad();
                HasMany(x => x.Images).AsList(x => x.Column("Pos")).Cascade.AllDeleteOrphan().LazyLoad();
                Map(x => x.IsFlagged);
                Map(x => x.FlagMessage);
                Map(x => x.Notes);
                Map(x => x.DiscogsReleaseId);
                Map(x => x.DiscogsMasterId);
                Map(x => x.WikipediaPageName);
                HasMany(x => x.AdditionalFiles).AsList(x => x.Column("Pos")).Cascade.AllDeleteOrphan().LazyLoad();
                Map(x => x.DateAdded);
                Map(x => x.DateModified);
                Map(x => x.DateAudioModified);
                Map(x => x.Score);
                Map(x => x.DynamicRange);
                Map(x => x.ReplayGainAlbumGain);
                Map(x => x.ReplayGainAlbumPeak);
            }
        }

        public const string ThumbnailMimeType = "image/jpeg";
        public const int ThumbnailSize = 256;

        public virtual int Id { get; protected set; }
        public virtual IList<ReleaseArtist> Artists { get; protected set; }
        public virtual string JoinedAlbumArtists { get; set; }
        public virtual string Title { get; set; }
        public virtual string Country { get; set; }
        public virtual ReleaseDate ReleaseDate { get; set; }
        public virtual ReleaseDate OriginalReleaseDate { get; set; }
        public virtual int DiscCount { get; set; }

        public virtual string Genre { get; set; }
        public virtual bool HasGenre
        {
            get { return !string.IsNullOrEmpty(this.Genre); }
        }

        public virtual IList<Track> Tracklist { get; protected set; }

        public virtual string Label { get; set; }
        public virtual string CatalogNumber { get; set; }
        public virtual bool HasCatalogInformation
        {
            get { return !string.IsNullOrEmpty(this.Country) && !string.IsNullOrEmpty(this.Label) && !string.IsNullOrEmpty(this.CatalogNumber) && this.ReleaseDate.IsValid; }
        }

        public virtual byte[] Thumbnail { get; set; }
        public virtual IList<Image> Images { get; protected set; }

        public virtual bool IsFlagged { get; set; }
        public virtual string FlagMessage { get; set; }

        public virtual string Notes { get; set; }

        public virtual int DiscogsReleaseId { get; set; }
        public virtual int DiscogsMasterId { get; set; }
        public virtual string WikipediaPageName { get; set; }
        public virtual IList<ReleaseAdditionalFile> AdditionalFiles { get; set; }

        public virtual DateTime DateAdded { get; set; }
        public virtual DateTime DateModified { get; set; }
        public virtual DateTime DateAudioModified { get; set; }

        public virtual int Score { get; set; }
        public virtual double DynamicRange { get; set; }
        public virtual double ReplayGainAlbumGain { get; set; }
        public virtual double ReplayGainAlbumPeak { get; set; }

        public virtual bool HasTrackArtists
        {
            get { return this.Tracklist.Any(t => !string.IsNullOrEmpty(t.JoinedArtists)); }
        }

        public virtual ReleaseAdditionalFile TorrentFile
        {
            get { return this.AdditionalFiles.FirstOrDefault(f => f.Type == ReleaseAdditionalFileType.Torrent); }
        }

        public virtual Image MainImage
        {
            get { return this.Images.Where(i => i.IsMain).FirstOrDefault(); }
        }

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

        public virtual string GetDirectory(CollectionManager collectionManager)
        {
            return Path.GetDirectoryName(this.Tracklist[0].GetAbsoluteFilename(collectionManager));
        }

        public virtual void UpdateDynamicProperties()
        {
            this.Score = new ReleaseScorer(this).Score;
            this.DynamicRange = this.Tracklist.Select(t => t.DynamicRange).Average();
        }
    }
}
