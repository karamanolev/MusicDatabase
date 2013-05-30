using System;
using System.Net;
using System.Net.Sockets;

namespace MusicDatabase.Audio.Network
{
    public class DiscoveryServer
    {
        public static readonly string DiscoveryPayload = "mdd";
        public static readonly string DiscoveryResponsePayload = "mdr";
        public const int ServerPort = 65004;

        private UdpClient client;

        public DiscoveryServer()
        {
            this.client = new UdpClient(ServerPort);
        }

        public void Run()
        {
            try
            {
                while (true)
                {
                    IPEndPoint from = null;

                    string data;
                    try
                    {
                        data = System.Text.Encoding.UTF8.GetString(this.client.Receive(ref from));
                    }
                    catch (SocketException e)
                    {
                        if (e.ErrorCode == 10004) // this.Close();
                        {
                            break;
                        }
                        throw e;
                    }

                    if (data.StartsWith(DiscoveryPayload))
                    {
                        string[] parts = data.Split(' ');
                        string id = parts[1];

                        byte[] response = System.Text.Encoding.UTF8.GetBytes(DiscoveryResponsePayload + " " + id + " " + Environment.ProcessorCount);

                        try
                        {
                            this.client.Send(response, response.Length, from);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public void Close()
        {
            this.client.Close();
        }
    }
}
