using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MusicDatabase.Audio.Network;
using MusicDatabase.Engine;
using MusicDatabase.Advanced;
using MusicDatabase.Settings;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for NetworkEncodingStatusBox.xaml
    /// </summary>
    public partial class NetworkEncodingStatusBox : UserControl
    {
        public static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

        private DispatcherTimer timer;
        private DiscoveryServerDescriptor[] servers;

        private CollectionManager collectionManager;
        public CollectionManager CollectionManager
        {
            get
            {
                return this.collectionManager;
            }
            set
            {
                this.collectionManager = value;

                if (this.collectionManager.Settings.NetworkEncoding)
                {
                    this.Visibility = Visibility.Visible;

                    if (this.timer == null)
                    {
                        this.timer = new DispatcherTimer();
                        this.timer.Interval = RefreshInterval;
                        this.timer.Tick += new EventHandler(timer_Tick);
                        this.Refresh();
                        this.timer.Start();
                    }
                }
                else
                {
                    this.Visibility = Visibility.Collapsed;

                    if (this.timer != null)
                    {
                        this.timer.Stop();
                    }
                }
            }
        }

        public SettingsManager SettingsManager { get; set; }

        public DiscoveryServerDescriptor[] Servers
        {
            get
            {
                if (this.servers == null)
                {
                    this.servers = this.GetServerList();
                }
                return this.servers;
            }
        }

        public NetworkEncodingStatusBox()
        {
            InitializeComponent();

            this.Loaded += new RoutedEventHandler(NetworkEncodingStatusBox_Loaded);
            this.Unloaded += new RoutedEventHandler(NetworkEncodingStatusBox_Unloaded);
        }

        void NetworkEncodingStatusBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.timer != null && this.collectionManager.Settings.NetworkEncoding)
            {
                this.timer.Start();
            }
        }

        void NetworkEncodingStatusBox_Unloaded(object sender, RoutedEventArgs e)
        {
            if (this.timer != null)
            {
                this.timer.Stop();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private DiscoveryServerDescriptor[] GetServerList()
        {
            using (DiscoveryClient client = new DiscoveryClient())
            {
                return servers = client.Discover(TimeSpan.Zero);
            }
        }

        public void Refresh()
        {
            Task refreshTask = new Task(() =>
            {
                this.servers = this.GetServerList();

                this.Dispatcher.InvokeAction(() =>
                {
                    int threads = servers.Select(s => s.ThreadCount).Sum();

                    this.textStatus.Text = servers.Length + " servers, " + threads + "+" + this.SettingsManager.Settings.ActualLocalConcurrencyLevel + " threads";
                });
            });
            refreshTask.Start();
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            ServerDiscoveryWindow serverDiscoveryWindow = new ServerDiscoveryWindow();
            serverDiscoveryWindow.ShowDialog(Window.GetWindow(this));
        }
    }
}
