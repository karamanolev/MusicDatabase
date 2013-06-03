using System;
using System.Linq;
using MongoDB.Driver;

namespace MusicDatabase
{
    public partial class DatabaseConnectionParamsWindow : MusicDatabaseWindow
    {
        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public string ConnectionString
        {
            get
            {
                return "mongodb://" + this.textHost.Text;
            }
        }

        public DatabaseConnectionParamsWindow()
        {
            InitializeComponent();
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnTestConnection_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            string host = this.textHost.Text;
            string user = this.textUser.Text;
            string pass = this.textPass.Password;
            string db = this.textDatabase.Text;

            try
            {
                MongoClient client = new MongoClient(new MongoClientSettings()
                {
                    Server = new MongoServerAddress(host)
                });
                MongoServer server = client.GetServer();
                MongoDatabase database = server.GetDatabase("MusicDatabase");
                database.GetCollectionNames().ToArray();

                Dialogs.Inform("The connection was established successfully.");
            }
            catch (Exception ex)
            {
                Dialogs.Warn("Unsuccessful connection:" + Environment.NewLine + ex);
            }
        }
    }
}
