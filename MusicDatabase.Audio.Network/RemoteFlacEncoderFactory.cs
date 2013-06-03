using System;
using System.IO;
using System.Linq;
using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Audio.Flac;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Network
{
    public class RemoteFlacEncoderFactory : EncoderFactoryBase
    {
        private DiscoveryServerDescriptor[] servers;
        private int compressionLevel;
        private int concurrencylevel;

        public RemoteFlacEncoderFactory(DiscoveryServerDescriptor[] servers, int compressionLevel, int concurrencyLevel, bool calculateRg, bool calculateDr)
            : base(calculateRg, calculateDr)
        {
            this.concurrencylevel = concurrencyLevel;

            this.servers = servers;
            this.compressionLevel = compressionLevel;

            this.ThreadCount = this.concurrencylevel + servers.Select(s => s.ThreadCount).Sum();
        }

        public override void TryDeleteResult(IParallelTask _task)
        {
            FileEncodeTask task = (FileEncodeTask)_task;
            Utility.TryDeleteFile(task.TargetFilename);
            Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(task.TargetFilename));
        }

        protected override IEncoder CreateEncoderInternal(int threadNumber, FileEncodeTask task, IAudioSource audioSource)
        {
            if (threadNumber < this.concurrencylevel)
            {
                return new NativeFlacEncoder(audioSource, task.TargetFilename, task.Tag, compressionLevel, task.TrackGain, task.DrMeter);
            }

            threadNumber -= this.concurrencylevel;

            foreach (DiscoveryServerDescriptor server in this.servers)
            {
                if (threadNumber < server.ThreadCount)
                {
                    return new RemoteFlacEncoder(server.Address, audioSource, task.TargetFilename, task.Tag, this.compressionLevel, task.TrackGain, task.DrMeter);
                }
                threadNumber -= server.ThreadCount;
            }

            throw new ArgumentException("threadNumber is too large.");
        }
    }
}
