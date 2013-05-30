using System;
using System.Threading.Tasks;
using System.Windows;
using DiscogsNet.Api;
using DiscogsNet.Model;

namespace MusicDatabase.DiscogsLink
{
    /// <summary>
    /// Interaction logic for DiscogsSelectReleaseWindow.xaml
    /// </summary>
    public partial class DiscogsSelectReleaseWindow : MusicDatabaseWindow
    {
        private const string StatusNoRelease = "No release selected";
        private const string StatusInvalidId = "Invalid release: enter ID or URL from Discogs";
        private const string StatusLoading = "Loading...";
        private const string StatusError = "Error loading release";

        private Discogs3 discogs;

        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public DiscogsSelectReleaseWindow(string initialData = null)
        {
            this.discogs = new Discogs3();

            InitializeComponent();

            int releaseId = 0;
            if (releaseId == 0 && initialData != null)
            {
                releaseId = DiscogsUtility.GetReleaseId(initialData);
            }
            if (releaseId == 0 && Clipboard.ContainsText())
            {
                releaseId = DiscogsUtility.GetReleaseId(Clipboard.GetText());
            }

            if (releaseId != 0)
            {
                textReleaseId.Text = Clipboard.GetText();
                DownloadRelease(releaseId);
            }
            else
            {
                HideRelease(StatusNoRelease);
            }
        }

        private void ShowRelease(Release release)
        {
            textStatus.Visibility = Visibility.Hidden;
            releaseViewer.Visibility = Visibility.Visible;
            releaseViewer.DataContext = release;
        }

        private void HideRelease(string status)
        {
            textStatus.Visibility = Visibility.Visible;
            textStatus.Text = status;
            releaseViewer.Visibility = Visibility.Hidden;
            releaseViewer.DataContext = null;
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            int releaseId = DiscogsUtility.GetReleaseId(textReleaseId.Text);

            if (releaseId == 0)
            {
                HideRelease(StatusInvalidId);
                return;
            }

            DownloadRelease(releaseId);
        }

        private void DownloadRelease(int releaseId)
        {
            HideRelease(StatusLoading);
            btnGo.IsEnabled = false;

            Task<Release> downloadTask = new Task<Release>(delegate()
            {
                return this.discogs.GetRelease(releaseId);
            });
            downloadTask.ContinueWith(delegate(Task<Release> prevTask)
            {
                Dispatcher.Invoke(new Action(delegate()
                {
                    btnGo.IsEnabled = true;
                    if (prevTask.IsFaulted)
                    {
                        HideRelease(StatusError + ": " + prevTask.Exception.Message);
                    }
                    else
                    {
                        ShowRelease(prevTask.Result);
                    }
                }));
            });
            downloadTask.Start();
        }

        public Release Release
        {
            get
            {
                return (Release)releaseViewer.DataContext;
            }
            set
            {
                releaseViewer.DataContext = value;
            }
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            if (Release == null)
            {
                Dialogs.Warn("Please select a release to match tracks from!");
            }
            else
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
