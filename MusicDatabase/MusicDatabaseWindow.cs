using System;
using System.Windows;
using MusicDatabase.Engine;
using MusicDatabase.Settings;

namespace MusicDatabase
{
    public class MusicDatabaseWindow : Window
    {
        private CollectionManager collectionManager;
        private bool shouldDisposeCollectionManager = true;

        public ICollectionSessionFactory CollectionSessionFactory { get; private set; }
        public SettingsManager SettingsManager { get; private set; }

        public CollectionManager CollectionManager
        {
            get
            {
                if (this.collectionManager == null)
                {
                    if (this.CollectionSessionFactory == null)
                    {
                        throw new InvalidOperationException("Window was created without collection session factory, but a manager is requested.");
                    }
                    this.collectionManager = new CollectionManager(this.CollectionSessionFactory);
                }
                return this.collectionManager;
            }
            set
            {
                if (this.collectionManager != null)
                {
                    throw new InvalidOperationException("Window already has a collection manager.");
                }
                this.collectionManager = value;
                this.shouldDisposeCollectionManager = false;
            }
        }

        public virtual bool SaveWindowSettings
        {
            get { return true; }
        }

        public MusicDatabaseWindow()
            : this(null)
        {
        }

        public MusicDatabaseWindow(ICollectionSessionFactory collectionSessionFactory)
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Closed += new EventHandler(MusicDatabaseWindow_Closed);

            this.SettingsManager = new SettingsManager();
            if (this.SaveWindowSettings)
            {
                this.SettingsManager.ManageWindowSettings(this);
            }

            if (collectionSessionFactory != null)
            {
                this.CollectionSessionFactory = collectionSessionFactory;
            }

            CollectionManager.CollectionChanged += new EventHandler(CollectionManager_CollectionChanged);
            SettingsManager.SettingsChanged += new EventHandler(SettingsManager_SettingsChanged);
        }

        private void SettingsManager_SettingsChanged(object sender, EventArgs e)
        {
            this.SettingsManager.ClearCache();
            this.OnSettingsChanged();
        }

        protected virtual void OnSettingsChanged()
        {
            if (this.collectionManager != null)
            {
                this.collectionManager.ClearCache();
            }
            this.SettingsManager.ClearCache();
        }

        private void CollectionManager_CollectionChanged(object sender, EventArgs e)
        {
            this.OnMusicCollectionChanged();
        }

        protected virtual void OnMusicCollectionChanged()
        {
            if (this.collectionManager != null)
            {
                this.collectionManager.ClearCache();
            }
        }

        private void MusicDatabaseWindow_Closed(object sender, EventArgs e)
        {
            if (this.collectionManager != null)
            {
                if (this.shouldDisposeCollectionManager)
                {
                    this.collectionManager.Dispose();
                }
                this.collectionManager = null;
            }

            this.SettingsManager.Dispose();

            CollectionManager.CollectionChanged -= this.CollectionManager_CollectionChanged;
        }

        [Obsolete("Use ShowDialog(Window owner)")]
        public new bool? ShowDialog()
        {
            throw new InvalidOperationException("MusicDatabaseWindow must be shown using ShowDialog(Window owner).");
        }

        public bool? ShowDialog(Window owner)
        {
            this.Owner = owner;
            return base.ShowDialog();
        }

        [Obsolete("Use ShowDialog(Window owner)")]
        public new void Show()
        {
            throw new InvalidOperationException("MusicDatabaseWindow must be shown using ShowDialog(Window owner).");
        }

        public void Show(Window owner)
        {
            this.Owner = owner;
            base.Show();
        }
    }
}
