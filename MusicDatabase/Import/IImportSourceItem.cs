using CUETools.Codecs;
using MusicDatabase.Engine.Tagging;

namespace MusicDatabase.Import
{
    public interface IImportSourceItem
    {
        string Name { get; }
        AudioFileTag Tag { get; }

        IAudioSource GetAudioSource();
    }
}
