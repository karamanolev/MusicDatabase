using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using MusicDatabase.Audio.Network;

namespace MusicDatabase.Advanced
{
    /// <summary>
    /// Interaction logic for ServerDiscoveryWindow.xaml
    /// </summary>
    public partial class ServerDiscoveryWindow : MusicDatabaseWindow
    {
        private DispatcherTimer timer;

        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public ServerDiscoveryWindow()
        {
            InitializeComponent();

            this.timer = new DispatcherTimer();
            this.timer.Interval = NetworkEncodingStatusBox.RefreshInterval;
            this.timer.Tick += new EventHandler(timer_Tick);
            this.timer.Start();

            this.Refresh();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            this.Refresh();
        }

        public void Refresh()
        {
            Task refreshTask = new Task(() =>
            {
                DiscoveryServerDescriptor[] servers = null;
                using (DiscoveryClient client = new DiscoveryClient())
                {
                    servers = client.Discover(TimeSpan.Zero);
                }
                this.Dispatcher.InvokeAction(() =>
                {
                    int threads = servers.Select(s => s.ThreadCount).Sum();

                    this.textStatus.Text = servers.Length + " servers, " + threads + "+" + this.SettingsManager.Settings.ActualLocalConcurrencyLevel + " threads";

                    List<string> strings = new List<string>();
                    foreach (var server in servers)
                    {
                        strings.Add(server.Address + " (" + server.ThreadCount + " threads)");
                    }
                    strings.Sort();

                    this.listViewServers.Items.Clear();
                    foreach (var server in strings)
                    {
                        this.listViewServers.Items.Add(server);
                    }
                });
            });
            refreshTask.Start();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.timer.Stop();
        }
    }
}
