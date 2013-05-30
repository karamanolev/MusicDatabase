using System;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.EncodingTargets;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for EditCollectionSettingsWindow.xaml
    /// </summary>
    public partial class EditCollectionSettingsWindow : MusicDatabaseWindow
    {
        public EditCollectionSettingsWindow(ICollectionSessionFactory collectionSessionFactory)
            : base(collectionSessionFactory)
        {
            InitializeComponent();

            if (this.CollectionManager.Settings == null)
            {
                this.CollectionManager.Settings = CollectionSettings.CreateDefault();
            }
            this.PopulateUI(this.CollectionManager.Settings);
        }

        private void PopulateUI(CollectionSettings settings)
        {
            this.textMusicDirectory.Text = settings.MusicDirectory;
            this.textFileNamingPattern.Text = settings.FileNamingPattern;
            this.checkNetworkEncoding.IsChecked = settings.NetworkEncoding;
            this.textLocalThreads.Text = this.SettingsManager.Settings.LocalConcurrencyLevel.ToString();
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            int localThreads;
            if (!int.TryParse(this.textLocalThreads.Text, out localThreads))
            {
                Dialogs.Error("Invalid local threads count!");
            }

            this.CollectionManager.Settings.MusicDirectory = this.textMusicDirectory.Text;
            this.CollectionManager.Settings.FileNamingPattern = this.textFileNamingPattern.Text;
            this.CollectionManager.Settings.NetworkEncoding = this.checkNetworkEncoding.IsChecked == true;

            this.CollectionManager.SaveSettings();

            this.SettingsManager.Settings.LocalConcurrencyLevel = localThreads;
            this.SettingsManager.Save();

            MusicDatabase.Settings.SettingsManager.OnSettingsChanged();

            this.DialogResult = true;
        }

        private void btnEncodingTargets_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            EncodingTargetsSettingsWindow encodingTargetsSettingsWindow = new EncodingTargetsSettingsWindow(this.CollectionSessionFactory);
            encodingTargetsSettingsWindow.ShowDialog(this);
        }
    }
}
