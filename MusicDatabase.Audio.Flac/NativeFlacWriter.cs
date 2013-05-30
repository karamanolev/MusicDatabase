using System;
using System.IO;
using System.Runtime.InteropServices;
using CUETools.Codecs;

namespace MusicDatabase.Audio.Flac
{
    public class NativeFlacWriter : IAudioDest
    {
        #region Unmanaged Functions

        delegate void FlacEncoderWriteCallback(byte[] buffer, uint bytes, uint samples, uint currentFrame);
        delegate void FlacEncoderSeekCallback(long absoluteByteOffset);
        delegate long FlacEncoderTellCallback();

        private const CallingConvention DllCallingConvention = CallingConvention.Cdecl;

        [UnmanagedFunctionPointer(DllCallingConvention)]
        private delegate int FLAC__StreamEncoderWriteCallback(IntPtr encoder, IntPtr buffer, uint bytes, uint samples, uint current_frame, IntPtr client_data);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        private delegate int FLAC__StreamEncoderSeekCallback(IntPtr encoder, long absolute_byte_offset, IntPtr client_data);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        private delegate int FLAC__StreamEncoderTellCallback(IntPtr encoder, ref long absolute_byte_offset, IntPtr client_data);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        private delegate int FLAC__StreamEncoderMetadataCallback(IntPtr encoder, IntPtr metadata, IntPtr client_data);

        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern IntPtr FLAC__stream_encoder_new();
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_encoder_set_channels(IntPtr encoder, int value);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_encoder_set_bits_per_sample(IntPtr encoder, int value);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_encoder_set_sample_rate(IntPtr encoder, int value);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_encoder_set_total_samples_estimate(IntPtr encoder, long value);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_encoder_set_compression_level(IntPtr encoder, int value);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern int FLAC__stream_encoder_init_stream(IntPtr encoder, FLAC__StreamEncoderWriteCallback write_callback, FLAC__StreamEncoderSeekCallback seek_callback, FLAC__StreamEncoderTellCallback tell_callback, FLAC__StreamEncoderMetadataCallback metadata_callback, IntPtr client_data);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_encoder_process_interleaved(IntPtr encoder, int[,] buffer, int samples);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_encoder_finish(IntPtr encoder);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern void FLAC__stream_encoder_delete(IntPtr encoder);

        #endregion

        private FLAC__StreamEncoderWriteCallback internalWriteCallback;
        private FLAC__StreamEncoderSeekCallback internalSeekCallback;
        private FLAC__StreamEncoderTellCallback internalTellCallback;

        private IntPtr handle;
        private bool initialized, closed;
        private int compressionLevel;
        private long finalSampleCount;
        private AudioPCMConfig pcm;
        private byte[] outputBuffer;

        private string outputPath;
        private Stream outputStream;

        public int CompressionLevel
        {
            get { return this.compressionLevel; }
            set { this.compressionLevel = value; }
        }

        public long FinalSampleCount
        {
            set { this.finalSampleCount = value; }
        }

        public AudioPCMConfig PCM
        {
            get { return this.pcm; }
        }

        public long Padding
        {
            set { throw new NotImplementedException(); }
        }

        public string Path
        {
            get { return this.outputPath; }
        }

        public long BlockSize
        {
            set { }
        }

        public object Settings
        {
            get { return null; }
            set { }
        }

        public NativeFlacWriter(string path, AudioPCMConfig pcm)
        {
            this.compressionLevel = 5;
            this.pcm = pcm;

            this.outputPath = path;
            this.outputStream = File.Create(path);
        }

        public NativeFlacWriter(Stream outputStream, AudioPCMConfig pcm)
        {
            this.compressionLevel = 5;
            this.pcm = pcm;

            this.outputStream = outputStream;
        }

        public void Close()
        {
            if (!this.closed)
            {
                if (this.initialized)
                {
                    try
                    {
                        try
                        {
                            this.FinalizeEncoding();
                        }
                        finally
                        {
                            FLAC__stream_encoder_delete(this.handle);
                            this.handle = IntPtr.Zero;
                        }
                    }
                    finally
                    {
                        if (this.outputPath != null)
                        {
                            this.outputStream.Close();
                        }
                    }
                }

                this.closed = true;
            }
        }

        private void FinalizeEncoding()
        {
            if (!FLAC__stream_encoder_finish(this.handle))
            {
                throw new FlacException("Error finalizing encoder");
            }
        }

        public void Delete()
        {
            if (this.outputPath != null)
            {
                throw new InvalidOperationException("This writer was not created from file.");
            }

            if (!closed)
            {
                this.Close();
                File.Delete(this.outputPath);
            }
        }

        public void Write(AudioBuffer buffer)
        {
            if (this.closed)
            {
                throw new InvalidOperationException("Writer already closed.");
            }

            buffer.Prepare(this);

            this.EnsureInitialized();

            if (!FLAC__stream_encoder_process_interleaved(this.handle, buffer.Samples, buffer.Length))
            {
                throw new FlacException("Error processing data");
            }
        }

        private void EnsureOutputBufferSize(int requiredSize)
        {
            if (this.outputBuffer == null || this.outputBuffer.Length < requiredSize)
            {
                this.outputBuffer = new byte[requiredSize];
            }
        }

        private void EnsureInitialized()
        {
            if (!this.initialized)
            {
                this.handle = FLAC__stream_encoder_new();
                if (this.handle == IntPtr.Zero)
                {
                    throw new FlacException("Error allocating encoder");
                }

                if (!FLAC__stream_encoder_set_channels(this.handle, pcm.ChannelCount))
                {
                    throw new FlacException("Error setting channels");
                }
                if (!FLAC__stream_encoder_set_bits_per_sample(this.handle, pcm.BitsPerSample))
                {
                    throw new FlacException("Error setting bits per sample");
                }
                if (!FLAC__stream_encoder_set_sample_rate(this.handle, pcm.SampleRate))
                {
                    throw new FlacException("Error setting sample rate");
                }
                if (this.finalSampleCount != 0)
                {
                    if (!FLAC__stream_encoder_set_total_samples_estimate(this.handle, this.finalSampleCount))
                    {
                        throw new FlacException("Error setting total samples estimate");
                    }
                }
                if (!FLAC__stream_encoder_set_compression_level(this.handle, this.compressionLevel))
                {
                    throw new FlacException("Error setting compression level");
                }

                this.internalWriteCallback = this.WriteCallback;
                this.internalSeekCallback = this.SeekCallback;
                this.internalTellCallback = this.TellCallback;

                if (FLAC__stream_encoder_init_stream(this.handle, this.internalWriteCallback, this.internalSeekCallback, this.internalTellCallback, null, IntPtr.Zero) != 0)
                {
                    throw new FlacException("Error initializing encoder");
                }

                this.initialized = true;
            }
        }

        private int WriteCallback(IntPtr encoder, IntPtr buffer, uint bytes, uint samples, uint current_frame, IntPtr client_data)
        {
            try
            {
                int intBytes = (int)bytes;
                this.EnsureOutputBufferSize(intBytes);
                Marshal.Copy(buffer, this.outputBuffer, 0, intBytes);
                this.outputStream.Write(this.outputBuffer, 0, intBytes);
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        private int SeekCallback(IntPtr encoder, long absolute_byte_offset, IntPtr client_data)
        {
            try
            {
                this.outputStream.Position = absolute_byte_offset;
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        private int TellCallback(IntPtr encoder, ref long absolute_byte_offset, IntPtr client_data)
        {
            try
            {
                absolute_byte_offset = this.outputStream.Position;
                return 0;
            }
            catch
            {
                return 1;
            }
        }
    }
}
