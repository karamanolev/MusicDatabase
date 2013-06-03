using System;
using System.Linq;
using System.Threading.Tasks;
using DiscogsNet.Api;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Advanced
{
    /// <summary>
    /// Interaction logic for FillReleaseDiscogsMasterIdsWindow.xaml
    /// </summary>
    public partial class FillReleaseDiscogsMasterIdsWindow : MusicDatabaseWindow
    {
        private bool shouldCancel;
        private Task workerTask;

        public FillReleaseDiscogsMasterIdsWindow(ICollectionSessionFactory collectionSessionFactory)
            : base(collectionSessionFactory)
        {
            InitializeComponent();

            this.workerTask = new Task(this.Worker);
            this.workerTask.Start();
        }

        private void Worker()
        {
            Discogs3 discogs = new Discogs3();

            Release[] releases = this.CollectionManager.Releases.Where(r => r.DiscogsReleaseId != 0 && r.DiscogsMasterId == 0).ToArray();
            this.Dispatcher.BeginInvokeAction(delegate
            {
                this.progressBar.Maximum = releases.Length;
                this.textBox.Text += releases.Length + " releases to process." + Environment.NewLine;
                this.textBox.ScrollToEnd();
            });
            int processed = 0;
            foreach (var release in releases)
            {
                string releaseMessage;
                try
                {
                    this.Dispatcher.BeginInvokeAction(delegate
                    {
                        this.textBox.Text += (processed + 1) + "/" + releases.Length + ": Requesting " + release.DiscogsReleaseId + Environment.NewLine;
                        this.textBox.ScrollToEnd();
                    });

                    DiscogsNet.Model.Release discogsRelease = discogs.GetRelease(release.DiscogsReleaseId);
                    //using (var transaction = this.CollectionManager.BeginTransaction())
                    //{
                    release.DiscogsMasterId = discogsRelease.MasterId;
                    this.CollectionManager.Save(release);
                    //transaction.Commit();
                    //}

                    releaseMessage = (processed + 1) + "/" + releases.Length + ": Successful update";
                }
                catch (Exception e)
                {
                    releaseMessage = (processed + 1) + "/" + releases.Length + ": Error - " + e;
                }

                ++processed;

                this.Dispatcher.BeginInvokeAction(delegate
                {
                    this.textBox.Text += releaseMessage + Environment.NewLine;
                    this.textBox.ScrollToEnd();
                    this.progressBar.Value = processed;
                });

                if (this.shouldCancel)
                {
                    break;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.shouldCancel = true;
            this.workerTask.Wait();
            CollectionManagerGlobal.OnCollectionChanged();
        }
    }
}
