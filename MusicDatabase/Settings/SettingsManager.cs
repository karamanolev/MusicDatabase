using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using MongoDB.Bson;
using MusicDatabase.Settings.Entities;
using MongoDB.Bson.IO;
using System.IO;
using MongoDB.Bson.Serialization;

namespace MusicDatabase.Settings
{
    public class SettingsManager : IDisposable
    {
        private string settingsFileLocation;
        private MusicDatabaseSettings settings;

        public MusicDatabaseSettings Settings
        {
            get { return this.settings; }
        }

        public SettingsManager()
        {
            this.settingsFileLocation = Path.Combine(Path.GetDirectoryName(typeof(SettingsManager).Assembly.Location), "Settings.json");
            this.TryLoadData();
        }

        private void TryLoadData()
        {
            try
            {
                BsonReader reader = BsonReader.Create(File.ReadAllText(this.settingsFileLocation));
                this.settings = BsonSerializer.Deserialize<MusicDatabaseSettings>(reader);
            }
            catch
            {
                this.settings = MusicDatabaseSettings.CreateDefault();
                this.Save();
            }
        }

        public void ClearCache()
        {
            if (this.settingsFileLocation == null)
            {
                return;
            }

            this.TryLoadData();
        }

        public void Save()
        {
            File.WriteAllText(this.settingsFileLocation, this.settings.ToJson());
        }

        public void Dispose()
        {
            this.settings = null;
            this.settingsFileLocation = null;
        }

        public void ManageWindowSettings(Window window)
        {
            string typeName = window.GetType().Name;

            window.Initialized += (sender, e) =>
            {
                WindowSettings settings = this.Settings.WindowsSettings.Where(w => w.Name == typeName).FirstOrDefault();
                if (settings != null)
                {
                    window.Left = settings.X;
                    window.Top = settings.Y;
                    window.Width = settings.Width;
                    window.Height = settings.Height;
                    window.WindowState = settings.State;
                    window.WindowStartupLocation = WindowStartupLocation.Manual;
                }
            };

            window.Closing += WindowClosing;
        }

        private void WindowClosing(object sender, CancelEventArgs e)
        {
            Window window = (Window)sender;
            string typeName = window.GetType().Name;

            WindowSettings closingSettings = this.Settings.WindowsSettings.Where(w => w.Name == typeName).FirstOrDefault();
            if (closingSettings == null)
            {
                closingSettings = new WindowSettings()
                {
                    Name = typeName
                };
                this.Settings.WindowsSettings.Add(closingSettings);
            }

            if (window.WindowState == WindowState.Maximized)
            {
                closingSettings.X = (int)window.RestoreBounds.Left;
                closingSettings.Y = (int)window.RestoreBounds.Top;
                closingSettings.Width = (int)window.RestoreBounds.Width;
                closingSettings.Height = (int)window.RestoreBounds.Height;
                closingSettings.State = WindowState.Maximized;
            }
            else if (window.WindowState == WindowState.Minimized)
            {
                closingSettings.X = (int)window.RestoreBounds.Left;
                closingSettings.Y = (int)window.RestoreBounds.Top;
                closingSettings.Width = (int)window.RestoreBounds.Width;
                closingSettings.Height = (int)window.RestoreBounds.Height;
                closingSettings.State = WindowState.Normal;
            }
            else if (window.WindowState == WindowState.Normal)
            {
                closingSettings.X = (int)window.Left;
                closingSettings.Y = (int)window.Top;
                closingSettings.Width = (int)window.Width;
                closingSettings.Height = (int)window.Height;
                closingSettings.State = WindowState.Normal;
            }

            this.Save();
        }

        public static event EventHandler SettingsChanged;
        public static void OnSettingsChanged()
        {
            if (SettingsChanged != null)
            {
                SettingsChanged(null, EventArgs.Empty);
            }
        }
    }
}
