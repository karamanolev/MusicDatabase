using System;
using System.IO;
using System.Linq;
using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Audio.Mp3;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Network
{
    public class RemoteMp3EncoderFactory : IEncoderFactory
    {
        private DiscoveryServerDescriptor[] servers;
        private int vbrQuality;
        private int localConcurrency;
        private bool calculateDr;

        public int ThreadCount
        {
            get { return this.localConcurrency + servers.Select(s => s.ThreadCount).Sum(); }
        }

        public RemoteMp3EncoderFactory(DiscoveryServerDescriptor[] servers, int vbrQuality, int localConcurrency, bool calculateDr)
        {
            this.localConcurrency = localConcurrency;

            this.servers = servers;
            this.vbrQuality = vbrQuality;
            this.calculateDr = calculateDr;
        }

        public void TryDeleteResult(IParallelTask _task)
        {
            FileEncodeTask task = (FileEncodeTask)_task;
            Utility.TryDeleteFile(task.TargetFilename);
            Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(task.TargetFilename));
        }

        public IEncoder CreateEncoder(int threadNumber, IParallelTask _task)
        {
            FileEncodeTask task = (FileEncodeTask)_task;
            IAudioSource audioSource = task.AudioSourceLazy();
            task.TrackGain = DspHelper.CreateTrackGain(audioSource);
            if (this.calculateDr)
            {
                task.DrMeter = DspHelper.CreateDrMeter(audioSource);
            }

            if (audioSource == null)
            {
                throw new SkipEncodingItemException("Audio source is not supported.");
            }

            try
            {
                if (threadNumber < this.localConcurrency)
                {
                    return new LocalMp3Encoder(audioSource, task.TargetFilename, task.Tag, vbrQuality, task.TrackGain, task.DrMeter);
                }

                threadNumber -= Environment.ProcessorCount;

                foreach (DiscoveryServerDescriptor server in this.servers)
                {
                    if (threadNumber < server.ThreadCount)
                    {
                        return new RemoteMp3VbrEncoder(server.Address, audioSource, task.TargetFilename, task.Tag, this.vbrQuality, task.TrackGain, task.DrMeter);
                    }
                    threadNumber -= server.ThreadCount;
                }

                throw new ArgumentException("threadNumber is too large.");
            }
            catch
            {
                audioSource.Close();
                throw;
            }
        }
    }
}
