using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using MusicDatabase.Engine.Entities;
using System.Threading.Tasks;
using MusicDatabase.Engine;

namespace MusicDatabase.WikipediaLink
{
    /// <summary>
    /// Interaction logic for WikipediaImporterWindow.xaml
    /// </summary>
    public partial class WikipediaImporterWindow : MusicDatabaseWindow
    {
        private Release release;

        public string WikipediaPageName
        {
            get
            {
                return this.textPageTitle.Text;
            }
        }

        public bool ImportOriginalReleaseDate
        {
            get
            {
                return this.checkImportReleaseDate.IsChecked == true;
            }
        }

        public ReleaseDate OriginalReleaseDate
        {
            get
            {
                return ReleaseDate.Parse(this.textReleaseDate.Text);
            }
        }

        public WikipediaImporterWindow(Release release)
        {
            this.release = release;

            InitializeComponent();

            this.textOldReleaseDate.Text = "Current: " + (release.OriginalReleaseDate.IsValid ? release.OriginalReleaseDate.ToString() : "empty");

            this.PickPageName();
        }

        private async void PickPageName()
        {
            if (string.IsNullOrWhiteSpace(this.release.Title))
            {
                this.SetStatus(null);
                return;
            }

            this.SetStatus("Searching pages...");

            string result = await Task.Run<string>(() =>
            {
                List<string> pageNames = new List<string>();

                pageNames.Add(this.release.Title + " (" + this.release.JoinedAlbumArtists + " album)");
                pageNames.Add(this.release.Title + " (album)");
                pageNames.Add(this.release.Title);

                foreach (string pageName in pageNames)
                {
                    bool alreaadyHasAddress = false;

                    this.Dispatcher.InvokeAction(() =>
                    {
                        alreaadyHasAddress = !string.IsNullOrEmpty(this.textAddress.Text);
                    });

                    if (alreaadyHasAddress)
                    {
                        return null;
                    }

                    try
                    {
                        string url = WikipediaImporter.MakeSearchUrlFromPageName(pageName);

                        WebClient client = new WebClient();
                        client.Headers["User-Agent"] = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.1 (KHTML, like Gecko) Chrome/21.0.1180.89 Safari/537.1";
                        string page = client.DownloadString(url);

                        if (page.Contains("but consider checking the search results below to see whether the topic is already"))
                        {
                            continue;
                        }

                        return url;
                    }
                    catch (WebException)
                    {
                    }
                }

                return null;
            });

            if (string.IsNullOrEmpty(this.textAddress.Text))
            {
                if (result == null)
                {
                    this.SetStatus("Couldn't find Wikipedia page.");
                    this.LoadPage("http://wikipedia.org");
                }
                else
                {
                    this.SetStatus(null);
                    this.LoadPage(result);
                }
            }
            else
            {
                this.SetStatus(null);
            }
        }

        private void SetStatus(string status)
        {
            this.textStatus.Visibility = status == null ? Visibility.Collapsed : Visibility.Visible;
            this.textStatus.Text = status;
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            this.LoadPage(this.textAddress.Text);
        }

        private void LoadPage(string url)
        {
            if (!url.ToLower().StartsWith("http://"))
            {
                url = "http://" + url;
            }

            this.textAddress.Text = url;
            this.SetStatus("Loading...");
            this.webBrowser.Navigate(url);
        }

        private void webBrowser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            this.SetStatus(null);

            dynamic doc = webBrowser.Document;
            string html = doc.documentElement.InnerHtml;

            WikipediaAlbumParseResult parseResult = WikipediaAlbumParseResult.Parse(html);

            if (parseResult == null)
            {
                this.textPageTitle.Text = "";
                this.textReleaseDate.Items.Clear();
                this.textReleaseDate.Text = "";
            }
            else
            {
                this.textPageTitle.Text = parseResult.PageTitle;
                this.textReleaseDate.Items.Clear();
                foreach (ReleaseDate date in parseResult.ReleaseDates)
                {
                    this.textReleaseDate.Items.Add(date.ToString());
                }
                this.textReleaseDate.SelectedIndex = 0;
                this.checkImportReleaseDate.IsChecked = parseResult.ReleaseDates.Length > 0;
            }
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void textReleaseDate_TextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            this.checkImportReleaseDate.IsChecked = true;
        }
    }
}
