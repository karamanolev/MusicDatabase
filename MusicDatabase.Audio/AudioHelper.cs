using System;
using System.Collections.Generic;
using System.IO;
using CUETools.Codecs;

namespace MusicDatabase.Audio
{
    public class AudioHelper
    {
        private static Dictionary<string, Func<string, IAudioSource>> fileSourceResolvers = new Dictionary<string, Func<string, IAudioSource>>();
        private static Dictionary<string, Func<Stream, IAudioSource>> streamSourceResolvers = new Dictionary<string, Func<Stream, IAudioSource>>();

        public static void RegisterFormat(string extension, Func<string, IAudioSource> fileResolver, Func<Stream, IAudioSource> streamResolver)
        {
            fileSourceResolvers[extension] = fileResolver;
            streamSourceResolvers[extension] = streamResolver;
        }

        public static bool IsSupportedAudioSource(string file)
        {
            string ext = Path.GetExtension(file).ToLower();
            return fileSourceResolvers.ContainsKey(ext);
        }

        public static IAudioSource GetAudioSourceForFile(string file)
        {
            string ext = Path.GetExtension(file).ToLower();
            Func<string, IAudioSource> resolver;
            if (fileSourceResolvers.TryGetValue(ext, out resolver))
            {
                return resolver(file);
            }
            return null;
        }

        public static IAudioSource GetAudioSourceForStream(Stream stream, string extension)
        {
            Func<Stream, IAudioSource> resolver;
            if (streamSourceResolvers.TryGetValue(extension, out resolver))
            {
                return resolver(stream);
            }
            return null;
        }
    }
}
