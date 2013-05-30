using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;

namespace MusicDatabase.Audio.Network
{
    public class DiscoveryClient : IDisposable
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);

        private UdpClient client;

        public DiscoveryClient()
        {
            this.client = new UdpClient(0);
        }

        public void Dispose()
        {
            this.client.Close();
            this.client = null;
        }

        public DiscoveryServerDescriptor[] Discover(TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero)
            {
                timeout = DefaultTimeout;
            }

            int id = new Guid().GetHashCode();

            byte[] payload = System.Text.Encoding.UTF8.GetBytes(DiscoveryServer.DiscoveryPayload + " " + id);
            this.client.Send(payload, payload.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryServer.ServerPort));

            List<DiscoveryServerDescriptor> result = new List<DiscoveryServerDescriptor>();

            DateTime start = DateTime.Now;

            while (DateTime.Now.Subtract(start) < timeout)
            {
                if (this.client.Available > 0)
                {
                    IPEndPoint from = null;
                    string data = System.Text.Encoding.UTF8.GetString(this.client.Receive(ref from));
                    if (data.StartsWith(DiscoveryServer.DiscoveryResponsePayload))
                    {
                        string[] parts = data.Split(' ');
                        if (int.Parse(parts[1]) == id)
                        {
                            int threadCount = int.Parse(parts[2]);
                            result.Add(new DiscoveryServerDescriptor(from.Address, threadCount));
                        }
                    }
                }
                Thread.Sleep(20);
            }

            return result.ToArray();
        }
    }
}
