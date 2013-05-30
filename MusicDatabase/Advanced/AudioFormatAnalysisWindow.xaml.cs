using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace MusicDatabase.Advanced
{
    /// <summary>
    /// Interaction logic for AudioFormatAnalysisWindow.xaml
    /// </summary>
    public partial class AudioFormatAnalysisWindow : MusicDatabaseWindow
    {
        public class FormatBucket
        {
            public string Channels { get; set; }
            public string BitsPerSample { get; set; }
            public string SampleRate { get; set; }
            public int Tracks { get; set; }
        }

        public class FormatBucketCollection : ObservableCollection<FormatBucket>
        {
            public void Add(TagLib.File tagFile)
            {
                foreach (FormatBucket bucket in this)
                {
                    if (tagFile.Properties.AudioChannels.ToString() == bucket.Channels &&
                        tagFile.Properties.BitsPerSample.ToString() == bucket.BitsPerSample &&
                        tagFile.Properties.AudioSampleRate.ToString() == bucket.SampleRate)
                    {
                        ++bucket.Tracks;
                        return;
                    }
                }
                this.Add(new FormatBucket()
                {
                    Channels = tagFile.Properties.AudioChannels.ToString(),
                    SampleRate = tagFile.Properties.AudioSampleRate.ToString(),
                    BitsPerSample = tagFile.Properties.BitsPerSample.ToString(),
                    Tracks = 1
                });
            }

            public void Add(FormatBucketCollection formatCollection)
            {
                foreach (FormatBucket otherBucket in formatCollection)
                {
                    foreach (FormatBucket bucket in this)
                    {
                        if (otherBucket.Channels == bucket.Channels &&
                            otherBucket.BitsPerSample == bucket.BitsPerSample &&
                            otherBucket.SampleRate == bucket.SampleRate)
                        {
                            bucket.Tracks += otherBucket.Tracks;
                            return;
                        }
                    }
                    this.Add(new FormatBucket()
                    {
                        Channels = otherBucket.Channels,
                        SampleRate = otherBucket.SampleRate,
                        BitsPerSample = otherBucket.BitsPerSample,
                        Tracks = otherBucket.Tracks
                    });
                }
            }
        }

        public class FileBucket
        {
            public string Channels { get; set; }
            public string BitsPerSample { get; set; }
            public string SampleRate { get; set; }
            public string Filename { get; set; }
        }

        private FormatBucketCollection bucketCollection, localBuckets;
        private ObservableCollection<FileBucket> fileBuckets, localFileBuckets;

        public AudioFormatAnalysisWindow(ICollectionSessionFactory factory)
            : base(factory)
        {
            this.bucketCollection = new FormatBucketCollection();
            this.localBuckets = new FormatBucketCollection();
            this.fileBuckets = new ObservableCollection<FileBucket>();
            this.localFileBuckets = new ObservableCollection<FileBucket>();

            InitializeComponent();

            this.listFormats.ItemsSource = this.bucketCollection;
            this.listFiles.ItemsSource = this.fileBuckets;

            this.ProgressChanged += ProgressBarUpdater.CreateHandler(this.Dispatcher, this.busyIndicator, () => false, p =>
            {
                this.bucketCollection.Add(this.localBuckets);
                this.localBuckets.Clear();
                this.fileBuckets.AddRange(this.localFileBuckets);
                this.localFileBuckets.Clear();
            });
            new Task(this.CalculateAudioFormats).Start();
        }

        private void CalculateAudioFormats()
        {
            this.Dispatcher.Invoke(() => this.busyIndicator.IsBusy = true);

            string rootPath = this.CollectionManager.Settings.MusicDirectory;

            Release[] releases = this.CollectionManager.Releases.ToArray();
            double processed = 0;

            foreach (Release release in releases)
            {
                foreach (Track track in release.Tracklist)
                {
                    string file = Path.Combine(rootPath, track.RelativeFilename);
                    TagLib.File tagFile = TagLib.File.Create(file);
                    if (tagFile.Properties.AudioChannels != 2 || tagFile.Properties.BitsPerSample != 16 || tagFile.Properties.AudioSampleRate != 44100)
                    {
                        this.localFileBuckets.Add(new FileBucket()
                        {
                            BitsPerSample = tagFile.Properties.BitsPerSample.ToString(),
                            Channels = tagFile.Properties.AudioChannels.ToString(),
                            SampleRate = tagFile.Properties.AudioSampleRate.ToString(),
                            Filename = track.RelativeFilename
                        });
                    }
                    localBuckets.Add(tagFile);
                }

                ++processed;
                double progress = processed / releases.Length;
                this.ProgressChanged(this, new ProgressChangedEventArgs(progress));
            }

            this.Dispatcher.Invoke(() => this.busyIndicator.IsBusy = false);
        }

        private event EventHandler<ProgressChangedEventArgs> ProgressChanged;
    }
}
