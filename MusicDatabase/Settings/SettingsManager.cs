using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using MusicDatabase.Settings.Entities;
using NHibernate;
using NHibernate.Linq;

namespace MusicDatabase.Settings
{
    public class SettingsManager : IDisposable
    {
        private MusicDatabaseSettings settings;
        private ISession session;

        public MusicDatabaseSettings Settings
        {
            get
            {
                if (this.settings != null)
                {
                    return this.settings;
                }

                this.settings = this.session.Query<MusicDatabaseSettings>().FirstOrDefault();

                if (this.settings != null)
                {
                    return this.settings;
                }

                return this.settings = MusicDatabaseSettings.CreateDefault();
            }
        }

        public SettingsManager()
        {
            this.session = SettingsSessionFactory.CreateSession();
        }

        public void Save()
        {
            if (this.settings == null)
            {
                return;
            }

            using (var transaction = this.session.BeginTransaction())
            {
                this.session.SaveOrUpdate(this.settings);
                transaction.Commit();
            }
        }

        public void ClearCache()
        {
            this.session.Clear();
        }

        public void Dispose()
        {
            if (this.session == null)
            {
                this.session.Dispose();
                this.session = null;
            }
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

            window.Closing += window_Closing;
        }

        private void window_Closing(object sender, CancelEventArgs e)
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

            using (var transaction = this.session.BeginTransaction())
            {
                this.session.SaveOrUpdate(closingSettings);
                transaction.Commit();
            }
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
