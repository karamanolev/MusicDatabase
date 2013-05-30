using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using DiscogsImageType = DiscogsNet.Model.ImageType;
using DiscogsRelease = DiscogsNet.Model.Release;

namespace MusicDatabase.DiscogsLink
{
    class DiscogsReleaseMerger
    {
        private Window window;
        private CollectionManager collectionManager;
        private Release release;
        private ReleaseImagesEditor imagesEditor;
        private List<IEnumerable<Tuple<Track, string>>> discs;

        public DiscogsReleaseMerger(Window window, CollectionManager collectionManager, Release release, ReleaseImagesEditor imagesEditor)
        {
            this.window = window;
            this.collectionManager = collectionManager;
            this.release = release;
            this.imagesEditor = imagesEditor;
            this.discs = new List<IEnumerable<Tuple<Track, string>>>();
        }

        private void AddImageFromDiscogsAsync(DiscogsNet.Model.Image discogsImage)
        {
            byte[] imageData;
            try
            {
                WebClient webClient = new WebClient();
                imageData = webClient.DownloadData(discogsImage.Uri);
            }
            catch
            {
                Dialogs.Error("Error downloading image.");
                return;
            }

            this.window.Dispatcher.BeginInvoke(new Action(() =>
            {
                MusicDatabase.Engine.Entities.Image image = new MusicDatabase.Engine.Entities.Image()
                {
                    Extension = discogsImage.Aggregate.Extension,
                    IsMain = discogsImage.Type == DiscogsNet.Model.ImageType.Primary,
                    MimeType = discogsImage.Aggregate.MimeType,
                    Type = discogsImage.Type == DiscogsNet.Model.ImageType.Primary ? ImageType.FrontCover : ImageType.Other
                };
                if (this.imagesEditor.AddImage(image, imageData))
                {
                    this.release.Images.Add(image);
                }
            }));
        }

        public void AddDisc(IEnumerable<Tuple<Track, string>> disc)
        {
            this.discs.Add(disc);
        }

        public void Merge()
        {
            string discogsReleaseString = release.DiscogsReleaseId == 0 ? null : release.DiscogsReleaseId.ToString();

            DiscogsSelectReleaseWindow selectReleaseWindow = new DiscogsSelectReleaseWindow(discogsReleaseString);
            if (selectReleaseWindow.ShowDialog(this.window) == true && selectReleaseWindow.Release != null)
            {
                var discogsRelease = selectReleaseWindow.Release;

                DiscogsSelectImportOptions importOptionsWindow = new DiscogsSelectImportOptions();
                if (importOptionsWindow.ShowDialog(this.window) == true)
                {
                    if (importOptionsWindow.ImportTrackData)
                    {
                        if (!ImportTrackData(selectReleaseWindow))
                        {
                            return;
                        }
                    }
                    if (importOptionsWindow.ImportAlbumArtist)
                    {
                        ImportAlbumArtist(discogsRelease, importOptionsWindow);
                    }
                    if (importOptionsWindow.ImportImages)
                    {
                        ImportImages(discogsRelease);
                    }
                    if (importOptionsWindow.ImportReleaseDate)
                    {
                        this.release.ReleaseDate = ReleaseDate.Parse(discogsRelease.ReleaseDate);
                    }
                    if (importOptionsWindow.ImportTitle)
                    {
                        this.release.Title = discogsRelease.Title;
                    }
                    if (importOptionsWindow.ImportCatalogInformation)
                    {
                        this.release.Country = discogsRelease.Country;
                        this.release.Genre = discogsRelease.Genres.FirstOrDefault();
                        if (discogsRelease.Labels.Length >= 1)
                        {
                            var discogsLabel = discogsRelease.Labels[0];
                            if (discogsRelease.Labels.Length > 1)
                            {
                                DiscogsSelectLabelWindow selectLabelWindow = new DiscogsSelectLabelWindow(discogsRelease);
                                selectLabelWindow.Owner = this.window;
                                if (selectLabelWindow.ShowDialog() != true)
                                {
                                    return;
                                }
                                discogsLabel = selectLabelWindow.SelectedLabel;
                            }

                            this.release.Label = discogsLabel.Aggregate.NameFixed;
                            this.release.CatalogNumber = discogsLabel.CatalogNumber;
                        }
                    }
                    if (importOptionsWindow.ImportNotes)
                    {
                        this.release.Notes = discogsRelease.Notes;
                    }

                    this.release.DiscogsReleaseId = discogsRelease.Id;
                    this.release.DiscogsMasterId = discogsRelease.MasterId;
                }
            }
        }

        private void ImportImages(DiscogsRelease discogsRelease)
        {
            bool replaceImages = true;
            if (this.release.Images.Count > 0)
            {
                replaceImages = Dialogs.YesNoQuestion("Replace existing release images?");
            }

            if (replaceImages)
            {
                foreach (Image image in this.release.Images.ToArray())
                {
                    this.imagesEditor.RemoveImage(image);
                }
            }

            WaitWindow waitWindow = new WaitWindow("Downloading images. Please wait.");

            Progress<double> progress = new Progress<double>();

            waitWindow.ShowDialog(this.window, new Task(() =>
            {
                this.DownloadImagesAsync(discogsRelease, progress);
            }), progress);
        }

        private void DownloadImagesAsync(DiscogsRelease discogsRelease, IProgress<double> progress)
        {
            var images = discogsRelease.Images.OrderBy(i => i.Type == DiscogsImageType.Primary ? 0 : 1).ToArray();

            for (int i = 0; i < images.Length; ++i)
            {
                this.AddImageFromDiscogsAsync(images[i]);
                progress.Report((double)(i + 1) / images.Length);
            }
        }

        private bool ImportTrackData(DiscogsSelectReleaseWindow selectReleaseWindow)
        {
            int discNumber = 1;
            foreach (var disc in this.discs)
            {
                DiscogsMatchWindow matchWindow = new DiscogsMatchWindow(
                    this.collectionManager,
                    disc,
                    selectReleaseWindow.Release);

                if (matchWindow.ShowDialog(this.window) != true)
                {
                    return false;
                }

                ++discNumber;
            }
            return true;
        }

        private void ImportAlbumArtist(DiscogsRelease discogsRelease, DiscogsSelectImportOptions importOptionsWindow)
        {
            if (DiscogsUtility.IsVariousArtists(discogsRelease.Aggregate.JoinedArtistsFixed))
            {
                this.release.Artists.Clear();
                this.release.Artists.Add(new ReleaseArtist()
                {
                    Artist = this.collectionManager.GetOrCreateArtist(DiscogsUtility.StandardVariousArtistsName)
                });
                this.release.JoinedAlbumArtists = DiscogsUtility.StandardVariousArtistsName;
            }
            else
            {
                this.release.Artists.Clear();
                foreach (var discogsArtist in discogsRelease.Artists)
                {
                    this.release.Artists.Add(new ReleaseArtist()
                    {
                        Artist = this.collectionManager.GetOrCreateArtist(importOptionsWindow.FixNames ? discogsArtist.Aggregate.NameVariationFixed : discogsArtist.Aggregate.NameVariationWithFallback),
                        JoinString = discogsArtist.Join
                    });
                }
                this.release.JoinedAlbumArtists = importOptionsWindow.FixNames ? discogsRelease.Aggregate.JoinedArtistsFixed : discogsRelease.Aggregate.JoinedArtists;
            }
        }
    }
}
