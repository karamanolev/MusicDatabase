using System;
using CUETools.Codecs;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio
{
    public class AudioChecksumCalculator
    {
        private IAudioSource audioSource;

        public AudioChecksumCalculator(string file)
        {
            this.audioSource = AudioHelper.GetAudioSourceForFile(file);
        }

        public AudioChecksumCalculator(IAudioSource source)
        {
            this.audioSource = source;
        }

        public uint GetCRC32()
        {
            try
            {
                uint checksum = 0;

                long totalSamples = this.audioSource.Length;
                long processedSamples = 0;

                AudioBuffer buffer = new AudioBuffer(this.audioSource.PCM, 44100);
                while (this.audioSource.Read(buffer, 44100) > 0)
                {
                    checksum = Crc32.ComputeChecksum(checksum, buffer.Bytes, 0, buffer.ByteLength);

                    processedSamples += buffer.Length;

                    ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs((double)processedSamples / totalSamples);
                    this.OnProgressChanged(eventArgs);
                    if (eventArgs.Cancel)
                    {
                        return 0;
                    }
                }

                return checksum;
            }
            finally
            {
                this.audioSource.Close();
            }
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        private void OnProgressChanged(ProgressChangedEventArgs eventArgs)
        {
            if (this.ProgressChanged != null)
            {
                this.ProgressChanged(this, eventArgs);
            }
        }
    }
}
