using System;
using System.IO;
using System.Net.Sockets;
using CUETools.Codecs;
using MusicDatabase.Audio.Mp3;
using MusicDatabase.Audio.Flac;

namespace MusicDatabase.Audio.Network
{
    class EncodingServerHandler
    {
        public const int BufferSize = 65536;

        private IAudioDest audioDest;
        private Stream networkStream;
        private MemoryStream outputStream;
        private byte[] buffer;

        public EncodingServerHandler(NetworkStream networkStream)
        {
            this.networkStream = networkStream;
        }

        public void OnEncodingRequest(EncodingRequestMessage message)
        {
            this.outputStream = new MemoryStream();

            AudioPCMConfig pcm = new AudioPCMConfig(message.BitsPerSample, message.ChannelCount, message.SampleRate);
            if (message.OutputFormat == ".mp3")
            {
                this.audioDest = new LameWriter(outputStream, pcm)
                {
                    FinalSampleCount = message.FinalSampleCount,
                    Settings = LameWriterSettings.CreateVbr(message.CompressionLevel)
                };
            }
            else if (message.OutputFormat == ".flac")
            {
                this.audioDest = new NativeFlacWriter(outputStream, pcm) {
                    FinalSampleCount = message.FinalSampleCount,
                    CompressionLevel = message.CompressionLevel
                };
            }
        }

        public void OnInputData(int length)
        {
            if (this.buffer == null)
            {
                this.buffer = new byte[BufferSize];
            }

            while (length > 0)
            {
                int read = Math.Min(this.buffer.Length, length);
                this.networkStream.ReadBytes(buffer, 0, read);

                AudioBuffer audioBuffer = new AudioBuffer(this.audioDest.PCM, this.buffer, read / this.audioDest.PCM.BlockAlign);
                this.audioDest.Write(audioBuffer);

                length -= read;
            }
        }

        public void OnInputDataEnd()
        {
            if (this.buffer == null)
            {
                this.buffer = new byte[BufferSize];
            }

            this.audioDest.Close();

            outputStream.Position = 0;
            int remaining = (int)outputStream.Length;
            while (remaining > 0)
            {
                int read = outputStream.Read(this.buffer, 0, Math.Min(this.buffer.Length, remaining));

                this.networkStream.Write((int)NetworkMessageType.DataMessage);
                this.networkStream.Write(read);
                this.networkStream.Write(this.buffer, 0, read);

                remaining -= read;
            }

            this.networkStream.Write((int)NetworkMessageType.DataEnd);
        }
    }
}
