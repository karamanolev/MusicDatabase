using System;
using System.IO;
using CUETools.Codecs;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Encoding
{
    public class CueToolsFlacEncoderFactory : EncoderFactoryBase
    {
        public CueToolsFlacEncoderFactory(int threadCount, bool calculateRg, bool calculateDr)
            : base(calculateRg, calculateDr)
        {
            this.ThreadCount = threadCount;
        }

        public override void TryDeleteResult(IParallelTask _task)
        {
            FileEncodeTask task = (FileEncodeTask)_task;
            Utility.TryDeleteFile(task.TargetFilename);
            Utility.TryDeleteEmptyFoldersToTheRoot(Path.GetDirectoryName(task.TargetFilename));
        }

        protected override IEncoder CreateEncoderInternal(int threadNumber, FileEncodeTask task, IAudioSource audioSource)
        {
            return new CueToolsFlacEncoder(audioSource, task.TargetFilename, task.Tag, 8, task.TrackGain, task.DrMeter);
        }
    }
}
