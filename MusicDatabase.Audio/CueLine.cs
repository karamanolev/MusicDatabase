using System.Collections.Generic;
using System.Text;

namespace MusicDatabase.Audio
{
    public class CueLine
    {
        private string[] Parts;

        public CueLine(string line)
        {
            List<string> parts = new List<string>();
            bool quote = false;
            StringBuilder part = new StringBuilder();
            foreach (char c in line)
            {
                if (quote)
                {
                    if (c == '"')
                    {
                        quote = false;
                    }
                    else
                    {
                        part.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        quote = true;
                    }
                    else if (c == ' ')
                    {
                        if (part.Length > 0)
                        {
                            parts.Add(part.ToString());
                            part.Clear();
                        }
                    }
                    else
                    {
                        part.Append(c);
                    }
                }
            }
            if (part.Length > 0)
            {
                parts.Add(part.ToString());
                part.Clear();
            }
            Parts = parts.ToArray();
        }

        public string Command
        {
            get
            {
                if (Parts.Length == 0)
                {
                    return "";
                }
                return Parts[0];
            }
        }

        public string this[int line]
        {
            get
            {
                return Parts[line];
            }
        }

        public int Count
        {
            get
            {
                return Parts.Length;
            }
        }
    }
}
