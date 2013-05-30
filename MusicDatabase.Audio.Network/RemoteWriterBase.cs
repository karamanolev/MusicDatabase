using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using CUETools.Codecs;

namespace MusicDatabase.Audio.Network
{
    public abstract class RemoteWriterBase : IAudioDest
    {
        private string outputPath;
        private Stream outputStream;
        private bool closed = false, initialized = false;
        private AudioPCMConfig pcm;
        private uint finalSampleCount;
        private int compressionLevel;

        private IPEndPoint remoteEndpoint;
        private TcpClient client;
        private Stream networkStream;

        protected abstract string NetworkEncoderIdentifier { get; }

        public long BlockSize
        {
            set { }
        }

        public int CompressionLevel
        {
            get { return this.compressionLevel; }
            set { this.compressionLevel = value; }
        }

        public long FinalSampleCount
        {
            set
            {
                if (value > uint.MaxValue)
                {
                    throw new ArgumentException("Input file too big.");
                }
                this.finalSampleCount = (uint)value;
            }
        }

        public AudioPCMConfig PCM
        {
            get { return this.pcm; }
        }

        public long Padding
        {
            set { }
        }

        public string Path
        {
            get { return this.outputPath; }
        }

        public object Settings
        {
            get { return null; }
            set { }
        }

        public RemoteWriterBase(IPAddress remoteAddress, string path, AudioPCMConfig pcm)
        {
            this.remoteEndpoint = new IPEndPoint(remoteAddress, EncodingServer.Port);
            this.pcm = pcm;

            this.outputPath = path;
            this.outputStream = File.Create(path);
        }

        public RemoteWriterBase(IPAddress remoteAddress, Stream output, AudioPCMConfig pcm)
        {
            this.remoteEndpoint = new IPEndPoint(remoteAddress, EncodingServer.Port);
            this.outputStream = output;
            this.pcm = pcm;
        }

        private void FinalizeEncoding()
        {
            this.networkStream.Write((int)NetworkMessageType.DataEnd);

            byte[] receiveBuffer = new byte[65536];
            while (true)
            {
                NetworkMessageType messageType = (NetworkMessageType)this.networkStream.ReadInt32();
                if (messageType == NetworkMessageType.DataMessage)
                {
                    int length = this.networkStream.ReadInt32();
                    while (length > 0)
                    {
                        int read = Math.Min(receiveBuffer.Length, length);
                        this.networkStream.ReadBytes(receiveBuffer, 0, read);
                        this.outputStream.Write(receiveBuffer, 0, read);

                        length -= read;
                    }
                }
                else if (messageType == NetworkMessageType.DataEnd)
                {
                    break;
                }
            }
        }

        public void Close()
        {
            if (!this.closed)
            {
                if (this.initialized)
                {
                    try
                    {
                        try
                        {
                            this.FinalizeEncoding();
                        }
                        finally
                        {
                            this.client.Close();
                            this.networkStream = null;
                            this.client = null;
                        }
                    }
                    finally
                    {
                        if (this.outputPath != null)
                        {
                            this.outputStream.Close();
                        }
                    }
                }

                this.closed = true;
            }
        }

        private void EnsureInitialized()
        {
            if (!this.initialized)
            {
                this.client = new TcpClient();
                this.client.Connect(this.remoteEndpoint);
                this.networkStream = this.client.GetStream();

                EncodingRequestMessage message = new EncodingRequestMessage()
                {
                    ChannelCount = this.pcm.ChannelCount,
                    BitsPerSample = this.pcm.BitsPerSample,
                    SampleRate = this.pcm.SampleRate,
                    FinalSampleCount = this.finalSampleCount,
                    OutputFormat = this.NetworkEncoderIdentifier,
                    CompressionLevel = this.compressionLevel
                };
                this.networkStream.Write((int)NetworkMessageType.EncodingRequest);
                message.Write(this.networkStream);

                this.initialized = true;
            }
        }

        public void Delete()
        {
            if (this.outputPath != null)
            {
                throw new InvalidOperationException("This writer was not created from file.");
            }

            if (!closed)
            {
                this.Close();
                File.Delete(this.outputPath);
            }
        }

        public void Write(AudioBuffer buffer)
        {
            if (this.closed)
            {
                throw new InvalidOperationException("Writer already closed.");
            }

            buffer.Prepare(this);

            this.EnsureInitialized();

            this.networkStream.Write((int)NetworkMessageType.DataMessage);
            this.networkStream.Write(buffer.ByteLength);
            this.networkStream.Write(buffer.Bytes, 0, buffer.ByteLength);
        }
    }
}
