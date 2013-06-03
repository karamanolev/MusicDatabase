using System;
using CUETools.Codecs;
using MusicDatabase.Engine;
using System.Security.Cryptography;

namespace MusicDatabase.Audio
{
    public class AudioChecksumCalculator
    {
        private IAudioSource audioSource;

        public uint CRC32 { get; private set; }
        public byte[] SHA1 { get; private set; }

        public AudioChecksumCalculator(string file)
        {
            this.audioSource = AudioHelper.GetAudioSourceForFile(file);
        }

        public AudioChecksumCalculator(IAudioSource source)
        {
            this.audioSource = source;
        }

        public void ComputeHashes()
        {
            try
            {
                SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider();

                this.CRC32 = 0;

                long totalSamples = this.audioSource.Length;
                long processedSamples = 0;

                AudioBuffer buffer = new AudioBuffer(this.audioSource.PCM, 44100);
                while (this.audioSource.Read(buffer, 44100) > 0)
                {
                    byte[] bufferBytes = buffer.Bytes;
                    if (this.audioSource.Position == this.audioSource.Length)
                    {
                        sha1.TransformFinalBlock(bufferBytes, 0, buffer.ByteLength);
                    }
                    else
                    {
                        sha1.TransformBlock(bufferBytes, 0, buffer.ByteLength, null, 0);
                    }
                    this.CRC32 = Crc32.ComputeChecksum(this.CRC32, buffer.Bytes, 0, buffer.ByteLength);

                    processedSamples += buffer.Length;

                    ProgressChangedEventArgs eventArgs = new ProgressChangedEventArgs((double)processedSamples / totalSamples);
                    this.OnProgressChanged(eventArgs);
                    if (eventArgs.Cancel)
                    {
                        return;
                    }
                }

                this.SHA1 = sha1.Hash;
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
