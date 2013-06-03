using System;
using System.IO;
using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Flac
{
    public class NativeFlacEncoderFactory : EncoderFactoryBase
    {
        public int CompressionLevel { get; set; }

        public NativeFlacEncoderFactory(int compressionLevel, int threadCount, bool calculateRg, bool calculateDr)
            : base(calculateRg, calculateDr)
        {
            if (threadCount == 0)
            {
                threadCount = Environment.ProcessorCount;
            }
            this.ThreadCount = threadCount;

            this.CompressionLevel = compressionLevel;
        }

        public override void TryDeleteResult(IParallelTask _task)
        {
            FileEncodeTask task = (FileEncodeTask)_task;
            Utility.TryDeleteFile(task.TargetFilename);
            Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(task.TargetFilename));
        }

        protected override IEncoder CreateEncoderInternal(int threadNumber, FileEncodeTask task, IAudioSource audioSource)
        {
            return new NativeFlacEncoder(audioSource, task.TargetFilename, task.Tag, this.CompressionLevel, task.TrackGain, task.DrMeter);
        }
    }
}
