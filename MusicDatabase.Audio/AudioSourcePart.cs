using System;
using CUETools.Codecs;

namespace MusicDatabase.Audio
{
    public class AudioSourcePart : IAudioSource
    {
        private long sampleStart, sampleEnd;
        private IAudioSource audioSource;

        public AudioSourcePart(IAudioSource audioSource)
        {
            this.audioSource = audioSource;
        }

        public void SetLengthFrames(long frameStart, long frameEnd)
        {
            long sampleStart = audioSource.PCM.SampleRate * frameStart / 75;
            long sampleEnd = audioSource.PCM.SampleRate * frameEnd / 75;

            if (frameEnd == 0)
            {
                sampleEnd = audioSource.Length;
            }

            if (sampleStart < 0 || sampleEnd > audioSource.Length || sampleStart > sampleEnd)
            {
                throw new InvalidOperationException("Invalid frame length.");
            }

            if (audioSource.Position != 0)
            {
                throw new InvalidOperationException("Source must not be seeked before length is set.");
            }

            this.sampleStart = sampleStart;
            this.sampleEnd = sampleEnd;
            this.ProperSeek(this.sampleStart);
        }

        private void ProperSeek(long offset)
        {
            this.audioSource.Position = offset;
            if (this.audioSource.Position > offset)
            {
                throw new InvalidOperationException();
            }

            AudioBuffer buffer = new AudioBuffer(this.audioSource.PCM, 44100);
            while (this.audioSource.Position < offset)
            {
                this.audioSource.Read(buffer, (int)Math.Min(buffer.Size, offset - this.audioSource.Position));
            }
        }

        public void Close()
        {
            this.audioSource.Close();
        }

        public long Length
        {
            get { return sampleEnd - sampleStart; }
        }

        public AudioPCMConfig PCM
        {
            get { return audioSource.PCM; }
        }

        public string Path
        {
            get { return audioSource.Path; }
        }

        public long Position
        {
            get
            {
                return audioSource.Position - this.sampleStart;
            }
            set
            {
                this.ProperSeek(this.sampleStart + value);
            }
        }

        public int Read(AudioBuffer buffer, int maxLength)
        {
            return audioSource.Read(buffer, (int)Math.Min(maxLength, this.sampleEnd - audioSource.Position));
        }

        public long Remaining
        {
            get { return audioSource.Remaining - this.sampleEnd; }
        }
    }
}
