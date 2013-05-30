using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using MusicDatabase.Advanced;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Edit;
using System.Text;

namespace MusicDatabase
{
    public class MainCollectionViewOperations
    {
        private MainCollectionView mainCollectionView;

        private CollectionManager CollectionManager
        {
            get { return this.mainCollectionView.CollectionManager; }
        }

        public MainCollectionViewOperations(MainCollectionView mainCollectionView)
        {
            this.mainCollectionView = mainCollectionView;
        }

        public void ToggleReleaseFlag(Release release)
        {
            if (release.IsFlagged)
            {
                if (Dialogs.Confirm("Unflag release?"))
                {
                    release.IsFlagged = false;
                    using (var transaction = this.CollectionManager.BeginTransaction())
                    {
                        this.CollectionManager.SaveOrUpdate(release);
                        transaction.Commit();
                    }
                    CollectionManager.OnCollectionChanged();
                }
            }
            else
            {
                FlagReleaseWindow flagWindow = new FlagReleaseWindow();
                flagWindow.Owner = Window.GetWindow(this.mainCollectionView);
                flagWindow.FlagMessage = release.FlagMessage;
                if (flagWindow.ShowDialog() == true)
                {
                    release.IsFlagged = true;
                    release.FlagMessage = flagWindow.FlagMessage;
                    using (var transaction = this.CollectionManager.BeginTransaction())
                    {
                        this.CollectionManager.SaveOrUpdate(release);
                        transaction.Commit();
                    }
                    CollectionManager.OnCollectionChanged();
                }
            }
        }

        public void DeleteRelease(Release release)
        {
            VerificationWindow verify = new VerificationWindow("This will permanently delete all files and images!");
            if (verify.ShowDialog(Window.GetWindow(this.mainCollectionView)) == true)
            {
                using (var transaction = this.CollectionManager.BeginTransaction())
                {
                    string releaseName = release.JoinedAlbumArtists + " - " + release.Title;
                    if (!this.CollectionManager.DeleteRelease(release))
                    {
                        Dialogs.Inform("Some files from " + releaseName + " were not deleted successfully. Please check by hand.");
                    }
                    transaction.Commit();
                }
                CollectionManager.OnCollectionChanged();
            }
        }

        public void PlayRelease(Release release)
        {
            string tempPath = Path.GetTempPath();
            string playlistPath = Path.Combine(tempPath, "MusicDatabasePlaylist.m3u8");
            File.WriteAllText(playlistPath, M3UGenerator.GeneratePlaylist(release, release.Tracklist, this.CollectionManager.Settings.MusicDirectory), Encoding.UTF8);
            Process.Start(playlistPath);
        }

        public void ComputeReleaseChecksum(Release release)
        {
            AudioChecksumWindow checksumWindow = new AudioChecksumWindow();
            foreach (Track track in release.Tracklist)
            {
                checksumWindow.AddItem(track.GetAbsoluteFilename(this.CollectionManager));
            }
            checksumWindow.ShowDialog(Window.GetWindow(this.mainCollectionView));
        }

        public void ExploreRelease(Release release)
        {
            Process.Start(release.GetDirectory(this.CollectionManager));
        }

        public void EditRelease(int id)
        {
            EditReleaseWindow editReleaseWindow = new EditReleaseWindow(this.mainCollectionView.CollectionSessionFactory, id);
            editReleaseWindow.ShowDialog(Window.GetWindow(this.mainCollectionView));
        }

        public void RemoveRelease(Release release)
        {
            VerificationWindow verify = new VerificationWindow("This will permanently delete all metadata, but audio files will be kept!");
            if (verify.ShowDialog(Window.GetWindow(this.mainCollectionView)) == true)
            {
                using (var transaction = this.CollectionManager.BeginTransaction())
                {
                    string releaseName = release.JoinedAlbumArtists + " - " + release.Title;
                    try
                    {
                        this.CollectionManager.RemoveRelease(release);
                    }
                    catch
                    {
                        Dialogs.Inform("Some files from " + releaseName + " were not deleted successfully. Please check by hand.");
                    }
                    transaction.Commit();
                }
                CollectionManager.OnCollectionChanged();
            }
        }
    }
}
