using System.IO;
using System.Net;
using CUETools.Codecs;

namespace MusicDatabase.Audio.Network
{
    public class RemoteFlacWriter : RemoteWriterBase
    {
        protected override string NetworkEncoderIdentifier
        {
            get { return ".flac"; }
        }

        public RemoteFlacWriter(IPAddress remoteAddress, string outputPath, AudioPCMConfig pcm)
            : base(remoteAddress, outputPath, pcm)
        {
        }

        public RemoteFlacWriter(IPAddress remoteAddress, Stream output, AudioPCMConfig pcm)
            : base(remoteAddress, output, pcm)
        {
        }
    }
}
