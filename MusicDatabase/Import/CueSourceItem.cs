using CUETools.Codecs;
using MusicDatabase.Audio;
using System.IO;
using MusicDatabase.Engine.Tagging;

namespace MusicDatabase.Import
{
    class CueSourceItem : IImportSourceItem
    {
        private string targetFilename;
        private CueSheet cueSheet;
        private int trackIndex;

        public string Name
        {
            get
            {
                return (trackIndex + 1).ToString("00") + ". " + cueSheet.Tracks[trackIndex].Title +
                    " @ " + Path.GetFileName(cueSheet.Filename);
            }
        }

        public AudioFileTag Tag { get; private set; }

        public CueSourceItem(string targetFilename, CueSheet cueSheet, int trackIndex)
        {
            this.targetFilename = targetFilename;
            this.cueSheet = cueSheet;
            this.trackIndex = trackIndex;

            this.Tag = new AudioFileTag()
            {
                AlbumArtists = this.cueSheet.Performer,
                Artists = this.cueSheet.Tracks[this.trackIndex].Performer ?? this.cueSheet.Performer,
                Title = this.cueSheet.Tracks[this.trackIndex].Title,
                Album = this.cueSheet.Title,
                Track = this.trackIndex + 1,
                TrackCount = this.cueSheet.Tracks.Length
            };
        }

        public IAudioSource GetAudioSource()
        {
            IAudioSource fullFileSource = AudioHelper.GetAudioSourceForFile(this.targetFilename);
            AudioSourcePart partSource = new AudioSourcePart(fullFileSource);
            partSource.SetLengthFrames(
                this.cueSheet.GetTrackStartFrame(this.trackIndex),
                this.cueSheet.GetTrackEndFrame(this.trackIndex));
            return partSource;
        }
    }
}
