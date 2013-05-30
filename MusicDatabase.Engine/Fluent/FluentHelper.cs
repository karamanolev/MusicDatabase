using System;
using System.Reflection;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace MusicDatabase.Engine.Fluent
{
    public static class FluentHelper
    {
        public static FluentConfiguration FinishConfiguration(this FluentConfiguration config, Assembly entitiesAssembly)
        {
            return config
                .Mappings(m => m.FluentMappings.AddFromAssembly(entitiesAssembly))
                .ExposeConfiguration(BuildSchema);
        }

        public static ISessionFactory CreateSessionFactory_SQLite(Assembly entitiesAssembly, string sqliteDbName)
        {
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard.UsingFile(sqliteDbName)).FinishConfiguration(entitiesAssembly).BuildSessionFactory();
        }

        public static Configuration CreateSessionFactory_SQLiteInMemory(Assembly entitiesAssembly)
        {
            return Fluently.Configure()
                .Database(SQLiteConfiguration.Standard.InMemory()).FinishConfiguration(entitiesAssembly).BuildConfiguration();
        }

        public static ISessionFactory CreateSessionFactory_MySQL(Assembly entitiesAssembly, string connectionString)
        {
            return Fluently.Configure()
                .Database(MySQLConfiguration.Standard.ConnectionString(connectionString)).FinishConfiguration(entitiesAssembly).BuildSessionFactory();
        }

        private static void BuildSchema(Configuration config)
        {
            new SchemaUpdate(config).Execute(false, true);
        }
    }
}
