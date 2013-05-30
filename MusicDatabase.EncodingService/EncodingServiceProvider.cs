using System;
using System.ComponentModel.Composition;
using ServiceHost;
using ServiceHost.Config;
using MusicDatabase.Audio.Network;
using System.Threading;

namespace MusicDatabase.EncodingService
{
    [Export(typeof(IGenericServiceProvider))]
    class EncodingServiceProvider : IGenericServiceProvider
    {
        private ILogger logger;

        private Thread discoveryServerThread, encodingServerThread;
        private DiscoveryServer discoveryServer;
        private EncodingServer encodingServer;

        public string Name
        {
            get { return "MusicDatabase.EncodingService"; }
        }

        public bool Initialize(ILogger logger, ConfigProviderBase configProvider)
        {
            this.logger = logger;

            try
            {
                this.discoveryServer = new DiscoveryServer();
            }
            catch (Exception e)
            {
                this.logger.Log("Error creating discovery server: " + e);
                return false;
            }
            this.discoveryServerThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    this.discoveryServer.Run();
                }
                catch (Exception e)
                {
                    this.logger.Log("Error running discovery server: " + e);
                }
            }));

            try
            {
                this.encodingServer = new EncodingServer();
            }
            catch (Exception e)
            {
                this.logger.Log("Error creating encoding server: " + e);
                return false;
            }
            this.encodingServerThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    this.encodingServer.Run();
                }
                catch (Exception e)
                {
                    this.logger.Log("Error running encoding server: " + e);
                }
            }));

            this.discoveryServerThread.Start();
            this.encodingServerThread.Start();

            this.logger.Log("Service started...");

            return true;
        }

        public bool Configure()
        {
            return true;
        }

        public void Dispose()
        {
            if (this.encodingServer != null)
            {
                this.encodingServer.Stop();
                this.encodingServerThread.Join();

                this.encodingServer = null;
                this.encodingServerThread = null;
            }

            if (this.discoveryServer != null)
            {
                this.discoveryServer.Close();
                this.discoveryServerThread.Join();

                this.discoveryServer = null;
                this.discoveryServerThread = null;
            }
        }
    }
}
