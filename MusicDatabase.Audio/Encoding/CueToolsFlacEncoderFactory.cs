using System;
using System.IO;
using CUETools.Codecs;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Encoding
{
    public class CueToolsFlacEncoderFactory : IEncoderFactory
    {
        public int ThreadCount { get; private set; }
        public bool CalculateDr { get; private set; }

        public CueToolsFlacEncoderFactory(int threadCount, bool calculateDr)
        {
            this.ThreadCount = threadCount;
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
                return new CueToolsFlacEncoder(audioSource, task.TargetFilename, task.Tag, 8, task.TrackGain, task.DrMeter);
            }
            catch
            {
                audioSource.Close();
                throw;
            }
        }
    }
}
