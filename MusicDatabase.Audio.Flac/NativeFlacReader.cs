using System;
using System.IO;
using System.Runtime.InteropServices;
using CUETools.Codecs;

namespace MusicDatabase.Audio.Flac
{
    public unsafe class NativeFlacReader : IAudioSource, IDisposable
    {
        #region Unmanaged Functions

        [StructLayout(LayoutKind.Sequential)]
        struct FLAC__FrameHeader
        {
            public uint Blocksize;
            public uint SampleRate;
            public uint Channels;
            public uint ChannelAssignment;
            public uint BitsPerSample;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct FLAC__Frame
        {
            public FLAC__FrameHeader Header;
        }

        enum FlacDecoderReadStatus
        {
            Continue = 0,
            EndOfStream = 1,
            Abort = 2
        }

        enum FlacDecoderSeekStatus
        {
            OK = 0,
            Error = 1,
            Unsupported = 2
        }

        enum FlacDecoderTellStatus
        {
            OK = 0,
            Error = 1,
            Unsupported = 2
        }

        enum FlacDecoderLengthStatus
        {
            OK = 0,
            Error = 1,
            Unsupported = 2
        }

        enum FlacDecoderWriteStatus
        {
            Continue = 0,
            Abort = 1
        }

        enum FlacDecoderErrorStatus
        {
            LostSync = 0,
            BadHeader = 1,
            FrameCrcMismatch = 2,
            UnparseableStream = 3
        }

        enum FlacDecoderState
        {
            SearchForMetadata = 0,
            ReadMetadata,
            SearchForFrameSync,
            ReadFrame,
            EndOfStream,
            OggError,
            SeekError,
            Aborted,
            MemoryAllocationError,
            Uninitialized
        }

        [UnmanagedFunctionPointer(DllCallingConvention)]
        delegate FlacDecoderReadStatus FlacDecoderReadCallback(IntPtr decoder, IntPtr buffer, ref uint bytes, IntPtr clientData);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        delegate FlacDecoderSeekStatus FlacDecoderSeekCallback(IntPtr decoder, long absoluteByteOffset, IntPtr clientData);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        delegate FlacDecoderTellStatus FlacDecoderTellCallback(IntPtr decoder, ref long absoluteByteOffset, IntPtr clientData);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        delegate FlacDecoderLengthStatus FlacDecoderLengthCallback(IntPtr decoder, ref long streamLength, IntPtr clientData);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        delegate bool FlacDecoderEofCallback(IntPtr decoder, IntPtr clientData);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        delegate FlacDecoderWriteStatus FlacDecoderWriteCallback(IntPtr decoder, ref FLAC__Frame frame, int** buffer, IntPtr clientData);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        delegate void FlacDecoderMetadataCallback(IntPtr decoder, IntPtr metadata, IntPtr clientData);
        [UnmanagedFunctionPointer(DllCallingConvention)]
        delegate void FlacDecoderErrorCallback(IntPtr decoder, FlacDecoderErrorStatus errorStatus, IntPtr clientData);

        private const CallingConvention DllCallingConvention = CallingConvention.Cdecl;

        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern IntPtr FLAC__stream_decoder_new();
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern int FLAC__stream_decoder_init_stream(IntPtr decoder, FlacDecoderReadCallback readCallback, FlacDecoderSeekCallback seekCallback, FlacDecoderTellCallback tellCallback, FlacDecoderLengthCallback lengthCallback, FlacDecoderEofCallback eofCallback, FlacDecoderWriteCallback writeCallback, FlacDecoderMetadataCallback metadataCallback, FlacDecoderErrorCallback errorCallback, IntPtr clientData);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_decoder_process_single(IntPtr decoder);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern long FLAC__stream_decoder_get_total_samples(IntPtr decoder);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_decoder_seek_absolute(IntPtr decoder, long sample);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_decoder_finish(IntPtr decoder);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern void FLAC__stream_decoder_delete(IntPtr decoder);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern FlacDecoderState FLAC__stream_decoder_get_state(IntPtr decoder);
        [DllImport("libFLAC", CallingConvention = DllCallingConvention)]
        private static extern bool FLAC__stream_decoder_set_md5_checking(IntPtr decoder, bool value);

        #endregion

        private string inputPath;
        private Stream inputStream;
        private IntPtr handle;
        private byte[] readBuffer;
        private AudioBuffer audioBuffer;
        private int audioBufferOffset = 0;
        private AudioPCMConfig pcm;
        private long decoderLength, decoderPosition = 0;
        private bool md5Checking = true;

        public bool MD5Checking
        {
            get { return this.md5Checking; }
            set
            {
                this.md5Checking = value;
                FLAC__stream_decoder_set_md5_checking(this.handle, value);
            }
        }

        private FlacDecoderReadCallback internalReadCallback;
        private FlacDecoderSeekCallback internalSeekCallback;
        private FlacDecoderTellCallback internalTellCallback;
        private FlacDecoderLengthCallback internalLengthCallback;
        private FlacDecoderEofCallback internalEofCallback;
        private FlacDecoderWriteCallback internalWriteCallback;
        private FlacDecoderErrorCallback internalErrorCallback;

        public NativeFlacReader(string file)
        {
            this.inputPath = file;
            this.inputStream = File.OpenRead(file);
            this.Init();
        }

        public NativeFlacReader(Stream input)
        {
            this.inputStream = input;
            this.Init();
        }

        private void Init()
        {
            this.internalReadCallback = this.ReadCallback;
            this.internalSeekCallback = this.SeekCallback;
            this.internalTellCallback = this.TellCallback;
            this.internalLengthCallback = this.LengthCallback;
            this.internalEofCallback = this.EofCallback;
            this.internalWriteCallback = this.WriteCallback;
            this.internalErrorCallback = this.ErrorCallback;

            this.handle = FLAC__stream_decoder_new();
            if (this.handle == IntPtr.Zero)
            {
                throw new FlacException("Error creating decoder");
            }

            int initStreamResult = FLAC__stream_decoder_init_stream(this.handle, this.internalReadCallback, this.internalSeekCallback, this.internalTellCallback, this.internalLengthCallback, this.internalEofCallback, this.internalWriteCallback, null, this.internalErrorCallback, IntPtr.Zero);
            if (initStreamResult != 0)
            {
                FLAC__stream_decoder_delete(this.handle);
                throw new FlacException("Error initializing decoder");
            }

            FLAC__stream_decoder_set_md5_checking(this.handle, this.md5Checking);

            while (this.pcm == null)
            {
                if (!FLAC__stream_decoder_process_single(this.handle))
                {
                    FLAC__stream_decoder_delete(this.handle);
                    throw new FlacException("Error reading up to metadata");
                }
            }
        }

        private FlacDecoderReadStatus ReadCallback(IntPtr decoder, IntPtr buffer, ref uint bytes, IntPtr clientData)
        {
            if (bytes == 0)
            {
                return FlacDecoderReadStatus.Abort;
            }
            if (this.readBuffer == null || this.readBuffer.Length < bytes)
            {
                this.readBuffer = new byte[Math.Max(bytes, 0x4000)];
            }
            bytes = (uint)this.inputStream.Read(this.readBuffer, 0, (int)bytes);
            if (bytes == 0)
            {
                return FlacDecoderReadStatus.EndOfStream;
            }

            Marshal.Copy(this.readBuffer, 0, buffer, (int)bytes);

            return FlacDecoderReadStatus.Continue;
        }

        private FlacDecoderSeekStatus SeekCallback(IntPtr decoder, long absoluteByteOffset, IntPtr clientData)
        {
            this.inputStream.Position = absoluteByteOffset;
            return FlacDecoderSeekStatus.OK;
        }

        private FlacDecoderTellStatus TellCallback(IntPtr decoder, ref long absoluteByteOffset, IntPtr clientData)
        {
            absoluteByteOffset = this.inputStream.Position;
            return FlacDecoderTellStatus.OK;
        }

        private FlacDecoderLengthStatus LengthCallback(IntPtr decoder, ref long streamLength, IntPtr clientData)
        {
            streamLength = this.inputStream.Length;
            return FlacDecoderLengthStatus.OK;
        }

        private bool EofCallback(IntPtr decoder, IntPtr clientData)
        {
            return this.inputStream.Position == this.inputStream.Length;
        }

        private FlacDecoderWriteStatus WriteCallback(IntPtr decoder, ref FLAC__Frame frame, int** buffer, IntPtr clientData)
        {
            int blocksize = (int)frame.Header.Blocksize;

            if (this.pcm == null)
            {
                this.pcm = new AudioPCMConfig((int)frame.Header.BitsPerSample, (int)frame.Header.Channels, (int)frame.Header.SampleRate);
                this.decoderLength = FLAC__stream_decoder_get_total_samples(this.handle);
            }

            if (this.audioBuffer == null || this.audioBuffer.Size < blocksize)
            {
                this.audioBuffer = new AudioBuffer(this.pcm, blocksize);
            }

            int channelCount = pcm.ChannelCount;
            for (int i = 0; i < channelCount; ++i)
            {
                fixed (int* sampleBufferPtr = this.audioBuffer.Samples)
                {
                    int* source = buffer[i];
                    int* sourceEnd = source + blocksize;

                    int* sampleBufferPtrCopy = sampleBufferPtr + i;

                    while (source != sourceEnd)
                    {
                        *sampleBufferPtrCopy = *source;

                        ++source;
                        sampleBufferPtrCopy += channelCount;
                    }
                }
            }

            this.audioBuffer.Length = blocksize;
            this.audioBufferOffset = 0;
            this.decoderPosition += blocksize;

            this.audioBuffer.Prepare(this.audioBuffer.Samples, this.audioBuffer.Length);

            return FlacDecoderWriteStatus.Continue;
        }

        private void ErrorCallback(IntPtr decoder, FlacDecoderErrorStatus errorStatus, IntPtr clientData)
        {
            switch (errorStatus)
            {
                case FlacDecoderErrorStatus.LostSync:
                    throw new FlacException("Synchronization was lost.");
                case FlacDecoderErrorStatus.BadHeader:
                    throw new FlacException("Encountered a corrupted frame header.");
                case FlacDecoderErrorStatus.FrameCrcMismatch:
                    throw new FlacException("Frame CRC mismatch.");
                default:
                    throw new FlacException("An unknown error has occurred.");
            }
        }

        public void Dispose()
        {
            this.Close();
        }

        #region IAudioSource

        public void Close()
        {
            if (this.inputPath != null && this.inputStream != null)
            {
                this.inputStream.Close();
                this.inputStream = null;
            }
            if (this.handle != IntPtr.Zero)
            {
                FLAC__stream_decoder_finish(this.handle);
                FLAC__stream_decoder_delete(this.handle);
                this.handle = IntPtr.Zero;
            }
        }

        public long Length
        {
            get { return this.decoderLength; }
        }

        public AudioPCMConfig PCM
        {
            get { return this.pcm; }
        }

        public string Path
        {
            get { return this.inputPath; }
        }

        public long Position
        {
            get
            {
                return this.decoderPosition - this.audioBuffer.Length + this.audioBufferOffset;
            }
            set
            {
                this.audioBufferOffset = 0;
                this.audioBuffer.Length = 0;
                this.decoderPosition = value;
                if (!FLAC__stream_decoder_seek_absolute(this.handle, value))
                {
                    throw new FlacException("Error seeking");
                }
            }
        }

        public int Read(AudioBuffer buffer, int maxLength)
        {
            buffer.Prepare(this, maxLength);
            int samplesCopied = 0;
            int bytesCopied = 0;

            Action copyBuffer = () =>
            {
                if (this.audioBuffer.Length > this.audioBufferOffset)
                {
                    int samplesToCopy = Math.Min(maxLength, this.audioBuffer.Length - this.audioBufferOffset);
                    int bytesToCopy = samplesToCopy * this.pcm.BlockAlign;

                    Array.Copy(this.audioBuffer.Bytes, this.audioBufferOffset * this.pcm.BlockAlign, buffer.Bytes, bytesCopied, bytesToCopy);

                    this.audioBufferOffset += samplesToCopy;
                    maxLength -= samplesToCopy;
                    samplesCopied += samplesToCopy;
                    bytesCopied += bytesToCopy;
                }
            };
            copyBuffer();

            while (maxLength > 0)
            {
                if (FLAC__stream_decoder_get_state(this.handle) == FlacDecoderState.EndOfStream)
                {
                    break;
                }
                if (!FLAC__stream_decoder_process_single(this.handle))
                {
                    throw new FlacException("Error processing frame.");
                }
                copyBuffer();
            }

            return samplesCopied;
        }

        public long Remaining
        {
            get { return this.Length - this.Position; }
        }

        #endregion
    }
}
