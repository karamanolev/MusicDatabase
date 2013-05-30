using System;
using System.Linq;
using CUETools.Codecs;

namespace MusicDatabase.Audio.Encoding
{
    public class DspEncoderFactory : IEncoderFactory
    {
        public int ThreadCount { get; private set; }
        public bool CalculateReplayGain { get; private set; }
        public bool CalculateDynamicRange { get; private set; }

        public DspEncoderFactory(int threadCount, bool calculateReplayGain, bool calculateDynamicRange)
        {
            this.ThreadCount = threadCount;
            this.CalculateReplayGain = calculateReplayGain;
            this.CalculateDynamicRange = calculateDynamicRange;
        }

        public IEncoder CreateEncoder(int threadNumber, IParallelTask _task)
        {
            if (_task is ReplayGainTask)
            {
                ReplayGainTask task = (ReplayGainTask)_task;
                return new ReplayGainTagEncoder(task);
            }
            else if (_task is FileEncodeTask)
            {
                FileEncodeTask task = (FileEncodeTask)_task;
                IAudioSource audioSource = task.AudioSourceLazy();
                if (audioSource == null)
                {
                    throw new SkipEncodingItemException("Unsupported audio format.");
                }
                if (this.CalculateReplayGain)
                {
                    task.TrackGain = DspHelper.CreateTrackGain(audioSource);
                }
                if (this.CalculateDynamicRange)
                {
                    task.DrMeter = DspHelper.CreateDrMeter(audioSource);
                }
                return new DspCalculatorEncoder(audioSource, task.TrackGain, task.DrMeter);
            }

            throw new NotSupportedException();
        }

        public void TryDeleteResult(IParallelTask _task)
        {
        }
    }
}
