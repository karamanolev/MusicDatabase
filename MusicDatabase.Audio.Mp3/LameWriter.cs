﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using CUETools.Codecs;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Mp3
{
    public class LameWriter : IAudioDest, IDisposable
    {
        #region Unmanaged Functions

        private const string LameDll = "libmp3lame";
        private const CallingConvention LameCallingConvention = CallingConvention.Cdecl;

        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern IntPtr lame_init();
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_close(IntPtr handle);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_num_channels(IntPtr handle, int channels);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_in_samplerate(IntPtr handle, int sampleRate);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_quality(IntPtr handle, int quality);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_VBR(IntPtr handle, int vbrMode);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_VBR_mean_bitrate_kbps(IntPtr handle, int meanBitrate);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_init_params(IntPtr handle);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_num_samples(IntPtr handle, uint numSamples);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_encode_buffer_interleaved(IntPtr handle, IntPtr pcm, int num_samples, IntPtr mp3buf, int mp3buf_size);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_encode_flush(IntPtr handle, IntPtr mp3buf, int size);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern uint lame_get_lametag_frame(IntPtr handle, IntPtr buffer, uint size);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_VBR_quality(IntPtr handle, float vbrQuality);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_brate(IntPtr handle, int bitrate);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_bWriteVbrTag(IntPtr handle, int writeVbrTag);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern int lame_set_write_id3tag_automatic(IntPtr handle, int automaticWriteId3Tag);
        [DllImport(LameDll, CallingConvention = LameCallingConvention)]
        private static extern IntPtr get_lame_version();

        #endregion

        public static string EncoderVersion
        {
            get
            {
                try
                {
                    IntPtr versionPtr = get_lame_version();
                    return Marshal.PtrToStringAnsi(versionPtr);
                }
                catch
                {
                    return "Unknown";
                }
            }
        }

        private string outputPath;
        private Stream outputStream;

        private LameWriterSettings settings;
        private bool closed = false, initialized = false;
        private IntPtr handle;
        private AudioPCMConfig pcm;
        private uint finalSampleCount;
        private byte[] outputBuffer;

        public long BlockSize
        {
            set { }
        }

        public int CompressionLevel
        {
            get { return 0; }
            set { }
        }

        public long FinalSampleCount
        {
            set
            {
                if (value > uint.MaxValue)
                {
                    throw new ArgumentException("Input file too big.");
                }
                this.finalSampleCount = (uint)value;
            }
        }

        public AudioPCMConfig PCM
        {
            get { return this.pcm; }
        }

        public long Padding
        {
            set { }
        }

        public string Path
        {
            get { return this.outputPath; }
        }

        public object Settings
        {
            get
            {
                return this.settings;
            }
            set
            {
                if (!(value is LameWriterSettings))
                {
                    throw new ArgumentException("Settings must be of type LameWriterSettings");
                }
                this.settings = (LameWriterSettings)value;
            }
        }

        public LameWriter(string path, AudioPCMConfig pcm)
        {
            this.CheckPCMConfig(pcm);

            this.pcm = pcm;
            this.outputPath = path;
            this.outputStream = File.Create(path);
            this.settings = new LameWriterSettings();
        }

        public LameWriter(Stream output, AudioPCMConfig pcm)
        {
            this.CheckPCMConfig(pcm);

            this.outputStream = output;
            this.pcm = pcm;
            this.settings = new LameWriterSettings();
        }

        private void CheckPCMConfig(AudioPCMConfig pcm)
        {
            if (pcm.BitsPerSample != 16 && pcm.BitsPerSample != 24)
            {
                throw new UnsupportedBitsPerSampleException("LAME only supports 16 bits/sample. 24 bits are supported through conversion.");
            }
        }

        private void FinalizeEncoding()
        {
            this.EnsureOutputBufferSize(7200);

            int flushResult;
            unsafe
            {
                fixed (byte* outputBufferPtr = outputBuffer)
                {
                    flushResult = lame_encode_flush(handle, (IntPtr)outputBufferPtr, outputBuffer.Length);
                }
            }
            if (flushResult < 0)
            {
                throw new LameException("Unknown flush error");
            }
            if (flushResult > 0)
            {
                this.outputStream.Write(this.outputBuffer, 0, flushResult);
            }

            int lametagFrameSize = this.GetLametagFrame();
            this.outputStream.Seek(0, SeekOrigin.Begin);
            this.outputStream.Write(this.outputBuffer, 0, lametagFrameSize);
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
                            lame_close(handle);
                        }
                    }
                    finally
                    {
                        handle = IntPtr.Zero;
                        if (this.outputPath != null)
                        {
                            this.outputStream.Close();
                        }
                    }
                }

                this.closed = true;
            }
        }

        public void Dispose()
        {
            this.Close();
        }

        private int GetLametagFrame()
        {
            while (true)
            {
                uint lametagFrameResult;
                unsafe
                {
                    fixed (byte* outputBufferPtr = outputBuffer)
                    {
                        lametagFrameResult = lame_get_lametag_frame(this.handle, (IntPtr)outputBufferPtr, (uint)outputBuffer.Length);
                    }
                }
                if (lametagFrameResult < 0)
                {
                    throw new LameException("Error getting lametag frame.");
                }
                if (lametagFrameResult <= outputBuffer.Length)
                {
                    return (int)lametagFrameResult;
                }
                this.EnsureOutputBufferSize((int)lametagFrameResult);
            }
        }

        public uint GetLametagFrame(byte[] outputBuffer)
        {
            unsafe
            {
                fixed (byte* outputBufferPtr = outputBuffer)
                {
                    return lame_get_lametag_frame(handle, (IntPtr)outputBufferPtr, (uint)outputBuffer.Length);
                }
            }
        }

        private void EnsureInitialized()
        {
            if (!this.initialized)
            {
                handle = lame_init();

                lame_set_bWriteVbrTag(handle, 1);
                lame_set_write_id3tag_automatic(handle, 0);

                lame_set_num_channels(handle, this.pcm.ChannelCount);
                lame_set_in_samplerate(handle, this.pcm.SampleRate);

                lame_set_quality(handle, (int)this.settings.Quality);

                if (this.finalSampleCount != 0)
                {
                    lame_set_num_samples(handle, this.finalSampleCount);
                }

                lame_set_VBR(this.handle, (int)this.settings.VbrMode);

                switch (this.settings.VbrMode)
                {
                    case LameVbrMode.Abr:
                        lame_set_VBR_mean_bitrate_kbps(handle, this.settings.Bitrate);
                        break;
                    case LameVbrMode.Default:
                        lame_set_VBR_quality(handle, this.settings.VbrQuality);
                        break;
                    case LameVbrMode.Off:
                        lame_set_brate(handle, this.settings.Bitrate);
                        break;
                    default:
                        throw new ArgumentException("Only ABR, Default and Off VBR modes are supported.");
                }

                if (lame_init_params(handle) != 0)
                {
                    throw new LameException("lame_init_params failed");
                }

                this.initialized = true;
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

        private void EnsureOutputBufferSize(int requiredSize)
        {
            if (this.outputBuffer == null || this.outputBuffer.Length < requiredSize)
            {
                this.outputBuffer = new byte[requiredSize];
            }
        }

        public void Write(AudioBuffer buffer)
        {
            if (this.closed)
            {
                throw new InvalidOperationException("Writer already closed.");
            }

            buffer.Prepare(this);

            if (buffer.PCM.BitsPerSample != 16)
            {
                if (buffer.PCM.BitsPerSample == 24)
                {
                    AudioBuffer newBuffer = new AudioBuffer(new AudioPCMConfig(16, buffer.PCM.ChannelCount, buffer.PCM.SampleRate), buffer.Size);
                    newBuffer.Length = buffer.Length;

                    int length0 = buffer.Samples.GetLength(0);
                    int length1 = buffer.Samples.GetLength(1);
                    for (int i = 0; i < length0; ++i)
                    {
                        for (int j = 0; j < length1; ++j)
                        {
                            newBuffer.Samples[i, j] = buffer.Samples[i, j] >> 8;
                        }
                    }
                    buffer = newBuffer;
                }
                else
                {
                    throw new NotSupportedException("Unsupported bit depth");
                }
            }

            this.EnsureInitialized();

            this.EnsureOutputBufferSize(buffer.Length * 5 / 4 + 7200);

            byte[] bytes = buffer.Bytes;

            int result;
            unsafe
            {
                fixed (byte* bytesPtr = bytes)
                {
                    fixed (byte* outputBufferPtr = this.outputBuffer)
                    {
                        result = lame_encode_buffer_interleaved(handle, (IntPtr)bytesPtr, buffer.Length, (IntPtr)outputBufferPtr, outputBuffer.Length);
                    }
                }
            }

            if (result < 0)
            {
                switch (result)
                {
                    case -1:
                        throw new LameException("Output buffer is too small");
                    case -2:
                        throw new LameException("malloc problem");
                    case -3:
                        throw new LameException("lame_init_params was not called");
                    case -4:
                        throw new LameException("Psycho acoustic problems");
                    default:
                        throw new LameException("Unknown error");
                }
            }

            if (result > 0)
            {
                this.outputStream.Write(this.outputBuffer, 0, result);
            }
        }
    }
}
