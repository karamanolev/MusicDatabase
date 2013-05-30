using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;
using MonoTorrent;
using MonoTorrent.Common;
using MusicDatabase.Engine;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for FileBox.xaml
    /// </summary>
    public partial class FileBox : UserControl
    {
        private string fileName;
        private byte[] fileBytes;

        public byte[] FileBytes
        {
            get { return this.fileBytes; }
        }

        public string FileName
        {
            get { return this.fileName; }
        }

        public void SetFile(string fileName, byte[] fileBytes)
        {
            this.fileName = fileName;
            this.fileBytes = fileBytes;
            this.UpdateUI();
        }

        public FileBox()
        {
            this.DataContext = this;
            InitializeComponent();
            this.UpdateUI();
        }

        private void UpdateUI()
        {
            if (this.FileBytes == null)
            {
                this.labelInfo.Text = "No File Present";
                this.labelInfo.ToolTip = null;
            }
            else
            {
                this.labelInfo.Inlines.Clear();
                this.labelInfo.Inlines.AddRange(this.GetFileDescription());
                this.labelInfo.ToolTip = this.FileName;
            }
        }

        private IEnumerable<Inline> GetFileDescription()
        {
            if (this.fileName != null && Path.GetExtension(this.fileName) == ".torrent")
            {
                Torrent torrent;
                if (Torrent.TryLoad(this.FileBytes, out torrent))
                {
                    string tracker = this.GetMainTracker(torrent);
                    string hyperlink = this.GetHyperlink(torrent);

                    if (hyperlink == null)
                    {
                        yield return new Run(tracker);
                    }
                    else
                    {
                        Hyperlink hyperlinkInline = new Hyperlink();
                        hyperlinkInline.Inlines.Add(new Run(tracker));
                        hyperlinkInline.NavigateUri = new Uri(hyperlink);
                        hyperlinkInline.RequestNavigate += this.hyperlinkInline_RequestNavigate;
                        yield return hyperlinkInline;
                    }

                    yield return new Run(" - " + torrent.Name);
                }
                else
                {
                    yield return new Run("Error Loading File");
                }
            }
            else
            {
                if (this.fileName != null)
                {
                    yield return new Run(Path.GetFileName(this.fileName) + " (" + Utility.BytesToString(this.FileBytes.Length) + ")");
                }
                else
                {
                    yield return new Run(Utility.BytesToString(this.FileBytes.Length));
                }
            }
        }

        private string GetMainTracker(Torrent torrent)
        {
            if (torrent.AnnounceUrls.Count > 0)
            {
                RawTrackerTier tier = torrent.AnnounceUrls[0];
                if (tier.Count > 0)
                {
                    Uri uri = new Uri(tier[0]);
                    string host = uri.Host;
                    string[] parts = host.Split('.');
                    if (parts.Length > 2)
                    {
                        host = parts[parts.Length - 2] + "." + parts[parts.Length - 1];
                    }
                    return host;
                }
                else
                {
                    return "No Tiers";
                }
            }
            else
            {
                return "No Announce URL";
            }
        }

        private void hyperlinkInline_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private string GetHyperlink(Torrent torrent)
        {
            if (this.GetMainTracker(torrent) == "rutracker.org")
            {
                return torrent.Comment;
            }
            return null;
        }
    }
}
