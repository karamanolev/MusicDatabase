using CUETools.Codecs;
using CUETools.Codecs.APE;
using CUETools.Codecs.WavPack;
using MusicDatabase.Audio;
using MusicDatabase.Audio.Flac;

namespace MusicDatabase
{
    class AudioSourceResolverRegisterer
    {
        public static void RegisterResolvers()
        {
            AudioHelper.RegisterFormat(".wav", file => new WAVReader(file, null), stream => new WAVReader(null, stream));
            AudioHelper.RegisterFormat(".ape", file => new APEReader(file, null), stream => new APEReader(null, stream));
            AudioHelper.RegisterFormat(".flac", file => new NativeFlacReader(file), stream => new NativeFlacReader(stream));
            AudioHelper.RegisterFormat(".wv", file => new WavPackReader(file, null), stream => new WavPackReader(null, stream));
        }
    }
}
