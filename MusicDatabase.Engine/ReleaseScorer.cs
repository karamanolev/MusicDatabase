using System;
using System.Collections.Generic;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    class ReleaseScorer
    {
        private const int NoLogPenalty = 10;
        private const int NoCuePenalty = 10;
        private const int NoImagesPenalty = 5;
        private const int NoDiscogsLinksPenalty = 2;
        private const int NoWikipediaLinkPenalty = 1;
        private const int NoCatalogInfoPenalty = 10;
        private const int NoReleaseDatePenalty = 10;

        private bool evaluated = false;
        private Release release;
        private List<Tuple<string, int>> penalties;

        public int Score
        {
            get
            {
                this.Calculate();
                return Math.Max(1, 100 - this.penalties.Sum(p => p.Item2));
            }
        }

        public Tuple<string, int>[] Penalties
        {
            get
            {
                this.Calculate();
                return this.penalties.ToArray();
            }
        }

        public ReleaseScorer(Release release)
        {
            this.release = release;
        }

        private void Penalize(string reason, int points)
        {
            this.penalties.Add(new Tuple<string, int>(reason, points));
        }

        private void Calculate()
        {
            if (this.evaluated)
            {
                return;
            }
            this.evaluated = true;

            this.penalties = new List<Tuple<string, int>>();
            this.EvaluateFiles();
            this.EvaluateImages();
            this.EvaluateLinks();
            this.EvaluateCatalogInfo();
        }

        private void EvaluateCatalogInfo()
        {
            if (string.IsNullOrEmpty(this.release.Label) || string.IsNullOrEmpty(this.release.CatalogNumber))
            {
                this.Penalize("No catalog information", NoCatalogInfoPenalty);
            }
            if (!this.release.ReleaseDate.IsValid)
            {
                this.Penalize("No release date", NoReleaseDatePenalty);
            }
            if (!this.release.OriginalReleaseDate.IsValid)
            {
                this.Penalize("No original release date", NoReleaseDatePenalty);
            }
        }

        private void EvaluateLinks()
        {
            if (this.release.DiscogsReleaseId == 0)
            {
                this.Penalize("No Discogs release", NoDiscogsLinksPenalty);
            }
            if (string.IsNullOrEmpty(this.release.WikipediaPageName))
            {
                this.Penalize("No Wikipedia page", NoWikipediaLinkPenalty);
            }
        }

        private void EvaluateImages()
        {
            if (!this.release.Images.Any())
            {
                this.Penalize("No images", NoImagesPenalty);
                return;
            }

            if (this.release.MainImage == null)
            {
                this.Penalize("No main image", NoImagesPenalty);
            }
        }

        private void EvaluateFiles()
        {
            if (!this.release.AdditionalFiles.Any(f => f.Type == ReleaseAdditionalFileType.Cue))
            {
                this.Penalize("No cue files", NoCuePenalty);
            }
            if (!this.release.AdditionalFiles.Any(f => f.Type == ReleaseAdditionalFileType.RipLog))
            {
                this.Penalize("No log files", NoLogPenalty);
            }
        }
    }
}
