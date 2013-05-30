using CUETools.Codecs;
using MusicDatabase.Audio;
using System.IO;
using MusicDatabase.Engine.Tagging;

namespace MusicDatabase.Import
{
    class FileSourceItem : IImportSourceItem
    {
        private string filename;

        public string Name
        {
            get { return Path.GetFileName(filename); }
        }

        public AudioFileTag Tag { get; private set; }

        public FileSourceItem(string filename)
        {
            this.filename = filename;
            try
            {
                this.Tag = new AudioFileTag(filename);
            }
            catch
            {
                this.Tag = new AudioFileTag();
            }
        }

        public IAudioSource GetAudioSource()
        {
            return AudioHelper.GetAudioSourceForFile(this.filename);
        }
    }
}
