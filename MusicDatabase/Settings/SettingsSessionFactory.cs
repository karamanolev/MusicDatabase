using System.IO;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Fluent;
using MusicDatabase.Settings.Entities;
using NHibernate;

namespace MusicDatabase.Settings
{
    public static class SettingsSessionFactory
    {
        private static ISessionFactory SessionFactory;

        public static ISession CreateSession()
        {
            if (SessionFactory == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Utility.SettingsDatabasePath));
                SessionFactory = FluentHelper.CreateSessionFactory_SQLite(typeof(MusicDatabaseSettings).Assembly, Utility.SettingsDatabasePath);
            }

            return SessionFactory.OpenSession();
        }
    }
}
