using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MusicDatabase.Audio.Network
{
    public class EncodingServer
    {
        public const int Port = 65003;

        private TcpListener listener;
        private Dictionary<TcpClient, Thread> threads;

        public EncodingServer()
        {
            this.threads = new Dictionary<TcpClient, Thread>();
            this.listener = new TcpListener(IPAddress.Any, Port);
            this.listener.Start();
        }

        public void Run()
        {
            while (true)
            {
                TcpClient client;
                try
                {
                    client = this.listener.AcceptTcpClient();
                }
                catch (SocketException e)
                {
                    if (e.ErrorCode == 10004) // this.Close()
                    {
                        break;
                    }
                    throw e;
                }

                Thread thread = new Thread(this.WorkerThread);
                thread.Priority = ThreadPriority.BelowNormal;
                this.threads[client] = thread;
                thread.Start(client);
            }
        }

        public void Stop()
        {
            this.listener.Stop();

            foreach (var thread in this.threads.ToArray())
            {
                thread.Key.Close();
                thread.Value.Join();
            }
        }

        private void WorkerThread(object arg)
        {
            TcpClient client = (TcpClient)arg;
            try
            {
                NetworkStream stream = client.GetStream();
                EncodingServerHandler handler = new EncodingServerHandler(stream);

                while (true)
                {
                    NetworkMessageType messageType;
                    try
                    {
                        messageType = (NetworkMessageType)stream.ReadInt32();
                    }
                    catch (EndOfStreamException)
                    {
                        break;
                    }

                    switch (messageType)
                    {
                        case NetworkMessageType.EncodingRequest:
                            EncodingRequestMessage message = new EncodingRequestMessage();
                            message.Read(stream);
                            handler.OnEncodingRequest(message);
                            break;
                        case NetworkMessageType.DataMessage:
                            int length = stream.ReadInt32();
                            handler.OnInputData(length);
                            break;
                        case NetworkMessageType.DataEnd:
                            handler.OnInputDataEnd();
                            break;
                    }
                }
            }
            catch
            {
            }
            finally
            {
                this.threads.Remove(client);
            }
        }
    }
}
