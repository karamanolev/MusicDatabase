using System;
using System.Linq;
using MySql.Data.MySqlClient;
using MusicDatabase.Engine;

namespace MusicDatabase
{
    public partial class MySQLConnectionParamsWindow : MusicDatabaseWindow
    {
        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public MySQLConnectionParamsWindow()
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
                MySqlConnection conn = new MySqlConnection(CollectionSessionFactory_MySQL.MakeConnectionString(host, user, pass, db));
                conn.Open();
                Dialogs.Inform("The connection was established successfully.");
            }
            catch (Exception ex)
            {
                Dialogs.Warn("Unsuccessful connection:" + Environment.NewLine + ex);
            }
        }
    }
}
