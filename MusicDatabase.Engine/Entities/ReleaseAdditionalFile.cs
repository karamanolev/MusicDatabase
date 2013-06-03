using System;
using System.Linq;

namespace MusicDatabase.Engine.Entities
{
    public class ReleaseAdditionalFile
    {
        public ReleaseAdditionalFileType Type { get; set; }
        public string OriginalFilename { get; set; }
        public string Description { get; set; }
        public byte[] File { get; set; }

        public ReleaseAdditionalFile()
        {
        }

        public override bool Equals(object obj)
        {
            ReleaseAdditionalFile file = obj as ReleaseAdditionalFile;
            if (file == null)
            {
                return false;
            }
            return
                file.Type == this.Type &&
                file.OriginalFilename == this.OriginalFilename &&
                file.Description == this.Description &&
                Enumerable.SequenceEqual(file.File, this.File);
        }

        public override int GetHashCode()
        {
            return Utility.GetCombinedHashCode(this.Type, this.OriginalFilename, this.Description, this.File);
        }

        public static string GetFilterForType(ReleaseAdditionalFileType type)
        {
            switch (type)
            {
                case ReleaseAdditionalFileType.Cue:
                    return "Cue Files (*.cue)|*.cue|" + Utility.AllFilesFilter;
                case ReleaseAdditionalFileType.Other:
                    return Utility.AllFilesFilter;
                case ReleaseAdditionalFileType.RipLog:
                    return "Log Files (*.log)|*.log|" + Utility.AllFilesFilter;
                case ReleaseAdditionalFileType.Torrent:
                    return "Torrent Files (*.torrent)|*.torrent|" + Utility.AllFilesFilter;
                default:
                    throw new NotSupportedException();
            }
        }

        public static ReleaseAdditionalFileType GetTypeForExtension(string extension)
        {
            switch (extension.ToLower())
            {
                case ".cue":
                    return ReleaseAdditionalFileType.Cue;
                case ".log":
                    return ReleaseAdditionalFileType.RipLog;
                case ".torrent":
                    return ReleaseAdditionalFileType.Torrent;
                default:
                    return ReleaseAdditionalFileType.Other;
            }
        }
    }
}
