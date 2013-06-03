using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.EncodingTargets
{
    /// <summary>
    /// Interaction logic for EncodingTargetsSettingsWindow.xaml
    /// </summary>
    public partial class EncodingTargetsSettingsWindow : MusicDatabaseWindow
    {
        public class EncodingTargetListViewItem
        {
            public EncodingTarget EncodingTarget { get; set; }
            public string Format { get; set; }
            public string Settings { get; set; }
        }

        private static readonly string[] VbrQualityDescriptions = new string[] { "Quality 0 (~245 kbps)", "Quality 1 (~225 kbps)", "Quality 2 (~190 kbps)", "Quality 3 (~175 kbps)", "Quality 4 (~165 kbps)", "Quality 5 (~130 kbps)", "Quality 6 (~115 kbps)", "Quality 7 (~100 kbps)", "Quality 8 (~85 kbps)", "Quality 9 (~65 kbps)" };

        private ObservableCollection<EncodingTargetListViewItem> items;

        public EncodingTargetsSettingsWindow(ICollectionSessionFactory collectionSessionFactory)
            : base(collectionSessionFactory)
        {
            this.items = new ObservableCollection<EncodingTargetListViewItem>();

            this.LoadItems();

            InitializeComponent();

            this.listViewTargets.ItemsSource = this.items;
        }

        private void LoadItems()
        {
            this.items.AddRange(this.CollectionManager.Settings.EncodingTargets.Select(t => this.CreateListViewItemFromTarget(t)));
        }

        private EncodingTargetListViewItem CreateListViewItemFromTarget(EncodingTarget target)
        {
            return new EncodingTargetListViewItem()
            {
                EncodingTarget = target,
                Format = "MP3",
                Settings = VbrQualityDescriptions[target.Mp3Settings.VbrQuality]
            };
        }

        private void btnAddTarget_Click(object sender, RoutedEventArgs e)
        {
            AddEncodingTargetWindow addTargetWindow = new AddEncodingTargetWindow();
            addTargetWindow.Owner = this;
            if (addTargetWindow.ShowDialog() == true)
            {
                EncodingTarget encodingTarget = new EncodingTarget()
                {
                    TargetDirectory = addTargetWindow.TargetDirectory,
                    FileNamingPattern = addTargetWindow.FileNamingPattern,
                    Mp3Settings = new EncodingTargetMp3Settings()
                    {
                        VbrQuality = addTargetWindow.VbrQuality
                    }
                };
                this.CollectionManager.Settings.EncodingTargets.Add(encodingTarget);
                this.items.Add(this.CreateListViewItemFromTarget(encodingTarget));
            }
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            this.CollectionManager.SaveSettings();
            CollectionManagerGlobal.OnCollectionChanged();

            this.DialogResult = true;
        }

        private void btnRemoveTarget_Click(object sender, RoutedEventArgs e)
        {
            int index = this.listViewTargets.SelectedIndex;
            if (index != -1)
            {
                this.CollectionManager.Settings.EncodingTargets.RemoveAt(index);
                this.CollectionManager.SaveSettings();

                this.items.RemoveAt(index);
            }
        }
    }
}
