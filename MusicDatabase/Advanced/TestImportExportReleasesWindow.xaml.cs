using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.Engine.ImportExport;

namespace MusicDatabase.Advanced
{
    /// <summary>
    /// Interaction logic for TestImportExportReleases.xaml
    /// </summary>
    public partial class TestImportExportReleases : MusicDatabaseWindow
    {
        class ReleasesNotEqualExceptio : Exception
        {
            public ReleasesNotEqualExceptio(string message)
                : base(message)
            {
            }
        }

        class ImportTester : CollectionImporterBase
        {
            public ImportTester(CollectionManager collectionManager, Action<Release, ICollectionImageHandler> updateThumbnailAction)
                : base(collectionManager, updateThumbnailAction)
            {
            }
        }

        class ReleaseNotEqualEventArgs : EventArgs
        {
            public string Differences { get; private set; }

            public ReleaseNotEqualEventArgs(string differences)
            {
                this.Differences = differences;
            }
        }

        class ExportTester : CollectionExporterBase, IDisposable
        {
            private CollectionManager memoryCollection;
            private CollectionSessionFactory_SQLiteMemory factory;

            private ReleaseEqualityComparer releaseComparer;
            private ImportTester importTester;

            public ExportTester(CollectionManager collectionManager)
                : base(collectionManager)
            {
                this.factory = new CollectionSessionFactory_SQLiteMemory();
                this.memoryCollection = new CollectionManager(this.factory);

                this.releaseComparer = new ReleaseEqualityComparer(true);
                this.importTester = new ImportTester(this.memoryCollection, UIHelper.UpdateReleaseThumbnail);
            }

            protected override IEnumerable<Stream> GetEntryOutputStream(string entryName, DateTime dateModified, object obj)
            {
                Release release = (Release)obj;

                using (MemoryStream stream = new MemoryStream())
                {
                    yield return stream;

                    stream.Position = 0;

                    Release importedRelease = this.importTester.ImportRelease(stream);

                    if (!this.releaseComparer.Equals(this.collectionManager, release, this.memoryCollection, importedRelease))
                    {
                        this.OnReleaseNotEqual(this.releaseComparer.DifferencesString);
                    }

                    ((MemoryImageHandler)this.memoryCollection.ImageHandler).Clear();
                }
            }

            private void OnReleaseNotEqual(string differencesString)
            {
                if (this.ReleaseNotEqual != null)
                {
                    this.ReleaseNotEqual(this, new ReleaseNotEqualEventArgs(differencesString));
                }
            }

            public override void Dispose()
            {
                base.Dispose();

                this.memoryCollection.Dispose();
                this.factory.Dispose();
            }

            public event EventHandler<ReleaseNotEqualEventArgs> ReleaseNotEqual;
        }

        private Progress<double> progress;

        public TestImportExportReleases(ICollectionSessionFactory collectionSessionFactory)
            : base(collectionSessionFactory)
        {
            InitializeComponent();

            this.progress = new Progress<double>();
            this.progress.ProgressChanged += progress_ProgressChanged;
            new Task(Scanner).Start();
        }

        private void progress_ProgressChanged(object sender, double e)
        {
            this.progressBar.Value = e;
        }

        private void Scanner()
        {
            ExportTester exportTester = new ExportTester(this.CollectionManager);
            exportTester.ReleaseNotEqual += (sender, e) =>
            {
                this.Dispatcher.BeginInvokeAction(() =>
                {
                    this.textDifferences.Text += "Releases differ" + e.Differences + Environment.NewLine;
                    this.textDifferences.ScrollToEnd();
                });
            };
            exportTester.Export(this.progress);
            this.Dispatcher.BeginInvokeAction(() =>
            {
                this.textDifferences.Text += "End of scan.";
                this.textDifferences.ScrollToEnd();
            });
        }
    }
}
