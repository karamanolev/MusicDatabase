using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MusicDatabase.Audio
{
    public class CueSheet
    {
        private CueLine[] Lines;

        public string Performer { get; private set; }
        public string Title { get; private set; }
        public string Filename { get; private set; }
        public CueTrack[] Tracks { get; private set; }

        public CueSheet(string cueFile)
        {
            List<CueTrack> tracks = new List<CueTrack>();
            Lines = File.ReadAllLines(cueFile).Select(l => new CueLine(l)).ToArray();
            CueTrack currentTrack = null;
            foreach (var line in Lines)
            {
                if (line.Command == "REM")
                {
                }
                else if (line.Command == "PERFORMER")
                {
                    if (currentTrack == null)
                    {
                        Performer = line[1];
                    }
                    else
                    {
                        currentTrack.Performer = line[1];
                    }
                }
                else if (line.Command == "TITLE")
                {
                    if (currentTrack == null)
                    {
                        Title = line[1];
                    }
                    else
                    {
                        currentTrack.Title = line[1];
                    }
                }
                else if (line.Command == "FILE")
                {
                    Filename = Path.Combine(Path.GetDirectoryName(cueFile), line[1]);
                }
                else if (line.Command == "TRACK")
                {
                    int track = int.Parse(line[1]);
                    if (track != tracks.Count + 1)
                    {
                        throw new FormatException();
                    }
                    currentTrack = new CueTrack();
                    tracks.Add(currentTrack);
                }
                else if (line.Command == "INDEX")
                {
                    int index = int.Parse(line[1]);

                    long time = ParseCdTime(line[2]);
                    currentTrack.Indexes[index] = time;
                }
                Tracks = tracks.ToArray();
            }
        }

        public string DiscoverTarget(out bool isOriginal)
        {
            if (File.Exists(this.Filename))
            {
                isOriginal = true;
                return this.Filename;
            }

            isOriginal = false;

            string withoutExtension = Path.GetFileNameWithoutExtension(this.Filename);
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(this.Filename)))
            {
                if (Path.GetFileNameWithoutExtension(file) == withoutExtension && AudioHelper.IsSupportedAudioSource(file))
                {
                    return file;
                }
            }

            return null;
        }

        public long GetTrackStartFrame(int index)
        {
            return this.Tracks[index].GetIndex(1);
        }

        public long GetTrackEndFrame(int index)
        {
            if (index < this.Tracks.Length - 1)
            {
                return this.Tracks[index + 1].GetIndex(1);
            }
            else
            {
                return 0;
            }
        }

        private static long ParseCdTime(string time)
        {
            string[] parts = time.Split(':');
            if (parts.Length != 3)
            {
                throw new FormatException();
            }
            long[] longs = parts.Select(p => long.Parse(p)).ToArray();
            long frames = 60 * 75 * longs[0] + 75 * longs[1] + longs[2];
            return frames;
        }
    }
}
