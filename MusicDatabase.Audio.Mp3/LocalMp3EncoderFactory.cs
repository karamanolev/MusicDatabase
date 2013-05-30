using System;
using System.IO;
using CUETools.Codecs;
using MusicDatabase.Audio.Encoding;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Mp3
{
    public class LocalMp3EncoderFactory : IEncoderFactory
    {
        private int threadCount;

        public int VbrQuality { get; set; }
        public bool CalculateDr { get; private set; }

        public int ThreadCount
        {
            get { return this.threadCount; }
        }

        public LocalMp3EncoderFactory(int vbrQuality, int threadCount, bool calculateDr)
        {
            this.threadCount = threadCount;

            this.VbrQuality = vbrQuality;
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
                return new LocalMp3Encoder(audioSource, task.TargetFilename, task.Tag, this.VbrQuality, task.TrackGain, task.DrMeter);
            }
            catch
            {
                audioSource.Close();
                throw;
            }
        }
    }
}
