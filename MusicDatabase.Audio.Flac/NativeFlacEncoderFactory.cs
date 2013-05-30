using System;
using System.IO;
using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Flac
{
    public class NativeFlacEncoderFactory : IEncoderFactory
    {
        private int threadCount;

        public int ThreadCount
        {
            get { return this.threadCount; }
        }

        public int CompressionLevel { get; set; }
        public bool CalculateDr { get; private set; }

        public NativeFlacEncoderFactory(int compressionLevel, int threadCount, bool calculateDr)
        {
            if (threadCount == 0)
            {
                threadCount = Environment.ProcessorCount;
            }
            this.threadCount = threadCount;

            this.CompressionLevel = compressionLevel;
            this.CalculateDr = calculateDr;
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
            if (this.CalculateDr)
            {
                task.DrMeter = DspHelper.CreateDrMeter(audioSource);
            }

            try
            {
                return new NativeFlacEncoder(audioSource, task.TargetFilename, task.Tag, this.CompressionLevel, task.TrackGain, task.DrMeter);
            }
            catch
            {
                audioSource.Close();
                throw;
            }
        }
    }
}
