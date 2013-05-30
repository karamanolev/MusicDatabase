using System.IO;

namespace MusicDatabase.Audio.Network
{
    class EncodingRequestMessage
    {
        public int ChannelCount { get; set; }
        public int BitsPerSample { get; set; }
        public int SampleRate { get; set; }
        public long FinalSampleCount { get; set; }

        public string OutputFormat { get; set; }
        public int CompressionLevel { get; set; }

        public void Read(Stream stream)
        {
            this.ChannelCount = stream.ReadInt32();
            this.BitsPerSample = stream.ReadInt32();
            this.SampleRate = stream.ReadInt32();
            this.FinalSampleCount = stream.ReadInt64();

            this.OutputFormat = stream.ReadString();
            this.CompressionLevel = stream.ReadInt32();
        }

        public void Write(Stream stream)
        {
            stream.Write(this.ChannelCount);
            stream.Write(this.BitsPerSample);
            stream.Write(this.SampleRate);
            stream.Write(this.FinalSampleCount);

            stream.Write(this.OutputFormat);
            stream.Write(this.CompressionLevel);
        }
    }
}
