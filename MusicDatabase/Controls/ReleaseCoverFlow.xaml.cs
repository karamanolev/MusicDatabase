using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ReleaseCoverFlow.xaml
    /// </summary>
    public partial class ReleaseCoverFlow : UserControl
    {
        public class ReleaseProxy
        {
            public string Title { get; set; }
            public string Country { get; set; }
            public string Label { get; set; }
            public ReleaseDate ReleaseDate { get; set; }
            public string Genre { get; set; }
            public bool HasGenre { get; set; }
            public bool HasCatalogInformation { get; set; }
            public byte[] Thumbnail { get; set; }

            public ReleaseProxy(Release release)
            {
                this.Title = release.Title;
                this.Country = release.Country;
                this.Label = release.Label;
                this.ReleaseDate = release.OriginalReleaseDate.IsValid ? release.OriginalReleaseDate : release.ReleaseDate;
                this.Genre = release.Genre;
                this.HasGenre = release.HasGenre;
                this.HasCatalogInformation = release.HasCatalogInformation;
                this.Thumbnail = release.Thumbnail;
            }
        }

        private Release[] releases;

        public Release[] Releases
        {
            get { return this.releases; }
            set
            {
                this.releases = value;
                this.UpdateUI();
            }
        }

        public int SelectedIndex
        {
            get { return this.coverFlow.SelectedIndex; }
            set { this.coverFlow.SelectedIndex = value; }
        }

        public ReleaseCoverFlow()
        {
            InitializeComponent();
        }

        private void UpdateUI()
        {
            if (releases == null)
            {
                this.coverFlow.ItemsSource = null;
            }
            else
            {
                this.coverFlow.ItemsSource = releases.Select(r => new ReleaseProxy(r));
            }
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.coverFlow.Clip = new RectangleGeometry()
            {
                Rect = new Rect(0, 0, this.coverFlow.ActualWidth, this.coverFlow.ActualHeight)
            };
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.OnItemSelected();
        }

        public event EventHandler ItemSelected;
        private void OnItemSelected()
        {
            if (this.ItemSelected != null)
            {
                this.ItemSelected(this, EventArgs.Empty);
            }
        }
    }
}
