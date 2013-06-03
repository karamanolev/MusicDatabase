using System;
using System.Linq;
using CUETools.Codecs;

namespace MusicDatabase.Audio.Encoding
{
    public abstract class EncoderFactoryBase : IEncoderFactory
    {
        public bool CalculateRg { get; private set; }
        public bool CalculateDr { get; private set; }
        public int ThreadCount { get; protected set; }

        public EncoderFactoryBase(bool calculateRg, bool calculateDr)
        {
            this.CalculateRg = calculateRg;
            this.CalculateDr = calculateDr;
        }

        protected IAudioSource SetupTask(FileEncodeTask task)
        {
            IAudioSource audioSource = task.AudioSourceLazy();
            if (audioSource == null)
            {
                throw new SkipEncodingItemException("Audio source is not supported.");
            }
            if (this.CalculateRg)
            {
                task.TrackGain = DspHelper.CreateTrackGain(audioSource);
            }
            if (this.CalculateDr)
            {
                task.DrMeter = DspHelper.CreateDrMeter(audioSource);
            }
            return audioSource;
        }

        public IEncoder CreateEncoder(int threadNumber, IParallelTask _task)
        {
            FileEncodeTask task = (FileEncodeTask)_task;
            IAudioSource audioSource = this.SetupTask(task);
            try
            {
                return this.CreateEncoderInternal(threadNumber, task, audioSource);
            }
            catch
            {
                if (audioSource != null)
                {
                    audioSource.Close();
                }
                throw;
            }
        }

        protected abstract IEncoder CreateEncoderInternal(int threadNumber, FileEncodeTask task, IAudioSource audioSource);
        public abstract void TryDeleteResult(IParallelTask task);
    }
}
