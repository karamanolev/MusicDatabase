using System;
using System.Data.SQLite;
using System.Linq;
using System.Threading;

namespace MusicDatabase.Engine.Database.SQLite
{
    public class SQLiteTransaction : ITransaction
    {
        private bool commited = false;
        private bool rolledBack = false;
        private SQLiteConnection connection;

        public SQLiteTransaction(SQLiteConnection connection)
        {
            this.connection = connection;
            Monitor.Enter(this.connection);
            this.connection.ExecuteCommand("BEGIN TRANSACTION");
        }

        public void Rollback()
        {
            if (this.commited || this.rolledBack)
            {
                throw new InvalidOperationException();
            }

            this.connection.ExecuteCommand("ROLLBACK TRANSACTION");
            this.rolledBack = true;
        }

        public void Dispose()
        {
            if (this.commited || this.rolledBack)
            {
                throw new InvalidOperationException();
            }

            this.connection.ExecuteCommand("COMMIT TRANSACTION");
            this.commited = true;
        }
    }
}
