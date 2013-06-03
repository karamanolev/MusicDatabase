using System;
using System.IO;
using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Mp3
{
    public class LocalMp3EncoderFactory : EncoderFactoryBase
    {
        public int VbrQuality { get; set; }

        public LocalMp3EncoderFactory(int vbrQuality, int threadCount, bool calculateRg, bool calculateDr)
            : base(calculateRg, calculateDr)
        {
            this.ThreadCount = threadCount;
            this.VbrQuality = vbrQuality;
        }

        public override void TryDeleteResult(IParallelTask _task)
        {
            FileEncodeTask task = (FileEncodeTask)_task;
            Utility.TryDeleteFile(task.TargetFilename);
            Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(task.TargetFilename));
        }

        protected override IEncoder CreateEncoderInternal(int threadNumber, FileEncodeTask task, IAudioSource audioSource)
        {
            return new LocalMp3Encoder(audioSource, task.TargetFilename, task.Tag, this.VbrQuality, task.TrackGain, task.DrMeter);
        }
    }
}
