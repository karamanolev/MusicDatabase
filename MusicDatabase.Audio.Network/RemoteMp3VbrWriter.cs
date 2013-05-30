using System;
using System.IO;
using System.Net;
using CUETools.Codecs;

namespace MusicDatabase.Audio.Network
{
    public class RemoteMp3VbrWriter : RemoteWriterBase
    {
        protected override string NetworkEncoderIdentifier
        {
            get { return ".mp3"; }
        }

        public RemoteMp3VbrWriter(IPAddress remoteAddress, string outputPath, AudioPCMConfig pcm)
            : base(remoteAddress, outputPath, pcm)
        {
            this.CheckPCMConfig(pcm);
        }

        public RemoteMp3VbrWriter(IPAddress remoteAddress, Stream output, AudioPCMConfig pcm)
            : base(remoteAddress, output, pcm)
        {
            this.CheckPCMConfig(pcm);
        }

        private void CheckPCMConfig(AudioPCMConfig pcm)
        {
            if (pcm.BitsPerSample != 16)
            {
                throw new ArgumentException("LAME only supports 16 bits/sample.");
            }
        }
    }
}
