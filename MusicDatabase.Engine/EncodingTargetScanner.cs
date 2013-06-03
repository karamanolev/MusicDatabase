using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public class EncodingTargetScanner
    {
        ICollectionManager collectionManager;
        private EncodingTarget encodingTarget;

        public EncodingTargetScanner(ICollectionManager collectionManager, EncodingTarget encodingTarget)
        {
            this.collectionManager = collectionManager;
            this.encodingTarget = encodingTarget;
        }

        public EncodingTargetScanResult Scan()
        {
            Release[] releases = this.collectionManager.Releases.ToArray();

            Dictionary<Release, List<Track>> tracksToEncode = new Dictionary<Release, List<Track>>();
            HashSet<Release> releasesToEncode = new HashSet<Release>(new CallbackEqualityComparer<Release>((r1, r2) => r1.Id == r2.Id));

            Action<Release, Track> addTrack = (Release _release, Track _track) =>
            {
                List<Track> list;
                if (tracksToEncode.TryGetValue(_release, out list))
                {
                    list.Add(_track);
                }
                else
                {
                    list = new List<Track>();
                    list.Add(_track);

                    tracksToEncode[_release] = list;
                    releasesToEncode.Add(_release);
                }
            };

            List<string> filesToDelete = new List<string>();

            HashSet<string> targetFiles = new HashSet<string>();

            int totalReleases = releases.Length;
            int processedReleases = 0;

            foreach (Release release in releases)
            {
                foreach (Track track in release.Tracklist)
                {
                    string targetFilename = Path.Combine(this.encodingTarget.TargetDirectory,
                        Path.ChangeExtension(track.RelativeFilename, this.encodingTarget.Extension));

                    if (File.Exists(targetFilename))
                    {
                        FileInfo original = new FileInfo(Path.Combine(this.collectionManager.Settings.MusicDirectory, track.RelativeFilename));
                        FileInfo encoding = new FileInfo(targetFilename);

                        if (original.LastWriteTime != encoding.LastWriteTime)
                        {
                            addTrack(release, track);
                        }
                    }
                    else
                    {
                        addTrack(release, track);
                    }

                    // This avoids a bug for example with N.O.H.A., as it normalizes the path
                    targetFiles.Add(Path.GetFullPath(targetFilename));
                }

                ++processedReleases;
                ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs((double)processedReleases / totalReleases);
                this.OnProgressChanged(eventArgs);
                if (eventArgs.Cancel)
                {
                    return null;
                }
            }

            if (Directory.Exists(this.encodingTarget.TargetDirectory))
            {
                string[] targetDirectoryFiles = Directory.GetFiles(this.encodingTarget.TargetDirectory, "*" + this.encodingTarget.Extension, SearchOption.AllDirectories);
                foreach (string file in targetDirectoryFiles)
                {
                    if (!targetFiles.Contains(file))
                    {
                        filesToDelete.Add(file);
                    }
                }
            }

            return new EncodingTargetScanResult()
            {
                TracksToEncode = tracksToEncode,
                ReleasesToEncode = releasesToEncode,
                FilesToDelete = filesToDelete.ToArray()
            };
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        private void OnProgressChanged(ProgressChangedEventArgs eventArgs)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, eventArgs);
            }
        }
    }
}
