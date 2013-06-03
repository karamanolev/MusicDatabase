using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media.Imaging;
using DiscogsNet;
using DiscogsNet.Api;
using DiscogsNet.Model;
using DiscogsNet.Model.Search;
using MusicDatabase.Engine;
using DatabaseImage = MusicDatabase.Engine.Entities.Image;
using DatabaseRelease = MusicDatabase.Engine.Entities.Release;
using ImageType = MusicDatabase.Engine.Entities.ImageType;

namespace MusicDatabase.Helpers
{
    class MissingCoverArtFillHelper : HelperBase
    {
        public class ReleaseData
        {
            public DatabaseRelease Release { get; set; }
            public List<ImageItem> Images { get; set; }

            public ReleaseData()
            {
                this.Images = new List<ImageItem>();
            }

            public void Deduplicate()
            {
                HashSet<string> hashes = new HashSet<string>();
                foreach (ImageItem image in this.Images.ToArray())
                {
                    string sha1 = Utility.ComputeSHA1(image.Data);
                    if (hashes.Contains(sha1))
                    {
                        this.Images.Remove(image);
                    }
                    else
                    {
                        hashes.Add(sha1);
                    }
                }
            }
        }

        public class ImageItem
        {
            public byte[] Data { get; set; }
            public Image Image { get; set; }
        }

        public class ImageViewModel
        {
            private ImageItem imageItem;
            private BitmapImage image;

            public byte[] Data
            {
                get { return this.imageItem.Data; }
            }
            public string Description { get; set; }
            public Image DiscogsImage { get; set; }

            public BitmapImage Image
            {
                get
                {
                    if (this.image == null)
                    {
                        try
                        {
                            this.image = new BitmapImage();
                            this.image.BeginInit();
                            this.image.StreamSource = new MemoryStream(this.Data);
                            this.image.EndInit();
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    return this.image;
                }
            }

            public ImageViewModel(ImageItem imageItem)
            {
                this.DiscogsImage = imageItem.Image;
                this.Description = imageItem.Image.Width + " x " + imageItem.Image.Height + " - " + Utility.BytesToString(imageItem.Data.Length);
                this.imageItem = imageItem;
            }
        }

        private IDiscogs3Api discogs = new Discogs3Cache(new Discogs3());
        private int totalReleases;
        private WaitWindow waitWindow;

        public MissingCoverArtFillHelper(MusicDatabaseWindow parentWindow, ICollectionSessionFactory collectionSessionFactory)
            : base(parentWindow, collectionSessionFactory)
        {
        }

        private bool MatchTracklist(DatabaseRelease release, Release discogsRelease)
        {
            if (release.Tracklist.Count != discogsRelease.Tracklist.Length)
            {
                return false;
            }

            int matches = 0;
            for (int i = 0; i < release.Tracklist.Count; ++i)
            {
                if (release.Tracklist[i].Title.ToLower() == discogsRelease.Tracklist[i].Title.ToLower())
                {
                    ++matches;
                }
            }

            return matches >= release.Tracklist.Count * 2 / 3;
        }

        private SearchResults RunSearch(DatabaseRelease release)
        {
            return this.discogs.Search(new SearchQuery()
               {
                   Type = SearchItemType.Release,
                   Artist = release.JoinedAlbumArtists,
                   ReleaseTitle = release.Title
               }, new PaginationRequest(1, 50));
        }

        private ReleaseData GetReleaseImages(DatabaseRelease release)
        {
            ReleaseData data = new ReleaseData();
            data.Release = release;

            var searchResult = this.RunSearch(release);
            var releaseResults = searchResult.Results.Cast<ReleaseSearchResult>().ToArray();
            List<ReleaseSearchResult> releasesToScan = new List<ReleaseSearchResult>();

            var firstReleases = releaseResults.Take(15).Select(r => this.discogs.GetRelease(r.Id)).ToArray();
            if (firstReleases.Any(r => this.MatchTracklist(release, r) && r.Images != null && r.Images.Length > 0))
            {
                foreach (ReleaseSearchResult releaseResult in releaseResults.Take(30))
                {
                    Release discogsRelease = this.discogs.GetRelease(releaseResult.Id);
                    if (this.MatchTracklist(release, discogsRelease) && discogsRelease.Images != null && discogsRelease.Images.Length > 0)
                    {
                        releasesToScan.Add(releaseResult);
                    }
                }
            }
            else
            {
                releasesToScan.AddRange(releaseResults);
            }

            foreach (var releaseResult in releasesToScan)
            {
                if (data.Images.Count >= 9)
                {
                    break;
                }

                var discogsRelease = discogs.GetRelease(releaseResult.Id);
                var image = discogsRelease.Aggregate.DesiredImage;
                if (image == null)
                {
                    continue;
                }
                var imageData = this.discogs.GetImage(image.Uri);

                data.Images.Add(new ImageItem()
                {
                    Image = image,
                    Data = imageData
                });
            }

            data.Deduplicate();

            return data;
        }

        private bool ChooseReleasePicture(ReleaseData releaseData, int releaseNumber)
        {
            List<ImageViewModel> images = new List<ImageViewModel>();
            foreach (ImageItem imageItem in releaseData.Images)
            {
                ImageViewModel imageViewModel = new ImageViewModel(imageItem);
                images.Add(imageViewModel);
            }

            string title = "Choose image for release " + releaseNumber + "/" + this.totalReleases + " - " + releaseData.Release.JoinedAlbumArtists + " - " + releaseData.Release.Title;

            bool pickResult = false;

            this.waitWindow.Dispatcher.Invoke(() =>
            {
                ImagePicker picker = new ImagePicker(title, images);
                if (picker.ShowDialog() == true)
                {
                    ImageViewModel selectedItem = (ImageViewModel)picker.SelectedItem;
                    this.UpdateImageForRelease(releaseData, selectedItem);
                    pickResult = true;
                }
                else if (picker.IsSkipped)
                {
                    pickResult = true;
                }
                else
                {
                    pickResult = false;
                }
            });

            return pickResult;
        }

        private void UpdateImageForRelease(ReleaseData releaseData, ImageViewModel selectedItem)
        {
            string extension = Path.GetExtension(selectedItem.DiscogsImage.Uri);
            string mimeType = MimeHelper.GetMimeTypeForExtension(extension);
            DatabaseImage image = new DatabaseImage()
            {
                Description = "Auto import from Discogs",
                Extension = extension,
                IsMain = true,
                MimeType = mimeType,
                Type = ImageType.FrontCover
            };

            releaseData.Release.Images.Add(image);
            releaseData.Release.DateModified = DateTime.Now;
            this.CollectionManager.ImageHandler.StoreImage(image, selectedItem.Data);
            ThumbnailGenerator.UpdateReleaseThumbnail(releaseData.Release, this.CollectionManager.ImageHandler);
            this.CollectionManager.Save(releaseData.Release);

            this.CollectionManager.Operations.WriteTags(releaseData.Release);
        }

        private void AddingThread(object arg)
        {
            BlockingCollection<ReleaseData> releaseData = (BlockingCollection<ReleaseData>)arg;

            var releases = this.CollectionManager.Releases.Where(r => r.Images.Count == 0).ToArray();
            this.totalReleases = releases.Length;

            foreach (var release in releases)
            {
                var releaseImages = this.GetReleaseImages(release);
                if (releaseImages.Images.Count > 0)
                {
                    releaseData.Add(releaseImages);
                }
            }

            releaseData.CompleteAdding();
        }

        public void Run()
        {
            this.waitWindow = new WaitWindow("Searching for cover art...");
            this.waitWindow.ShowDialog(this.ParentWindow, () =>
            {
                BlockingCollection<ReleaseData> releaseData = new BlockingCollection<ReleaseData>(6);

                Thread addingThread = new Thread(this.AddingThread);
                addingThread.Start(releaseData);

                for (int releaseNumber = 1; ; ++releaseNumber)
                {
                    ReleaseData release;
                    if (!releaseData.TryTake(out release, -1))
                    {
                        break;
                    }

                    if (!this.ChooseReleasePicture(release, releaseNumber))
                    {
                        addingThread.Abort();
                        break;
                    }
                }

                addingThread.Join();
            });

            CollectionManagerGlobal.OnCollectionChanged();
        }
    }
}
