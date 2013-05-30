using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using CsQuery;
using MusicDatabase.Engine.Entities;
using System.Collections.Generic;

namespace MusicDatabase.Engine
{
    public class WikipediaAlbumParseResult
    {
        public string PageTitle { get; private set; }
        public ReleaseDate[] ReleaseDates { get; private set; }

        public static WikipediaAlbumParseResult Parse(string data)
        {
            WikipediaAlbumParseResult result = new WikipediaAlbumParseResult();

            var dom = CQ.CreateFragment(data);

            var title = dom.Select("h1.firstHeading").Text();
            if (string.IsNullOrEmpty(title))
            {
                return null;
            }
            result.PageTitle = title;

            var published = dom.Select(".published").Html();
            published = Strip(published);

            try
            {
                result.ReleaseDates = ParseWikipediaReleaseDate(published);
            }
            catch
            {
                result.ReleaseDates = new ReleaseDate[] { };
            }

            return result;
        }

        private static string Strip(string text)
        {
            return Regex.Replace(text, @"<(.|\n)*?>", " ");
        }

        private static ReleaseDate[] ParseWikipediaReleaseDate(string published)
        {
            string[] parts = Regex.Split(published, "[^0-9a-zA-Z]").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            if (parts.Length == 1)
            {
                int year;
                if (int.TryParse(parts[0], out year))
                {
                    return new ReleaseDate[] {
                        new ReleaseDate(year)
                    };
                }
                return new ReleaseDate[] { };
            }
            else if (parts.Length == 2)
            {
                int part1 = ParseNumberOrMonth(parts[0]);
                int part2 = ParseNumberOrMonth(parts[1]);
                if (part1 > 50)
                {
                    return new ReleaseDate[] {
                        new ReleaseDate(part1, part2)
                    };
                }
                else
                {
                    return new ReleaseDate[] {
                        new ReleaseDate(part2, part1)
                    };
                }
            }
            else
            {
                HashSet<ReleaseDate> releaseDates = new HashSet<ReleaseDate>();

                for (int i = 0; i < parts.Length - 2; ++i)
                {
                    try
                    {
                        ReleaseDate date = ParseReleaseDateTriplet(new string[] { parts[i], parts[i + 1], parts[i + 2] });
                        if (date.IsValid)
                        {
                            releaseDates.Add(date);
                        }
                    }
                    catch
                    {
                    }
                }

                return releaseDates.ToArray();
            }
        }

        private static ReleaseDate ParseReleaseDateTriplet(string[] triplet)
        {
            int part0 = ParseNumberOrMonth(triplet[0]);
            int part1 = ParseNumberOrMonth(triplet[1]);
            int part2 = ParseNumberOrMonth(triplet[2]);

            if (part0 > 50)
            {
                if (IsMonth(triplet[1]))
                {
                    if (part2 == -1)
                    {
                        return new ReleaseDate(part0, part1);
                    }
                    return new ReleaseDate(part0, part1, part2);
                }
                else if (IsMonth(triplet[2]))
                {
                    return new ReleaseDate(part0, part2, part1);
                }
                return new ReleaseDate();
            }
            else if (part1 > 50)
            {
                if (IsMonth(triplet[0]) && part2 == -1)
                {
                    return new ReleaseDate(part1, part0);
                }
                else if (IsMonth(triplet[2]) && part0 == -1)
                {
                    return new ReleaseDate(part1, part2);
                }
            }
            else if (part2 > 50)
            {
                if (IsMonth(triplet[0]))
                {
                    return new ReleaseDate(part2, part0, part1);
                }
                else if (IsMonth(triplet[1]))
                {
                    if (part0 == -1)
                    {
                        return new ReleaseDate(part2, part1);
                    }
                    return new ReleaseDate(part2, part1, part0);
                }
            }

            return new ReleaseDate();
        }

        private static int ParseMonth(string month)
        {
            month = month.ToLower();

            int i = 1;
            foreach (string monthName in DateTimeFormatInfo.InvariantInfo.MonthNames)
            {
                if (string.IsNullOrEmpty(monthName) || monthName.Length < 3) continue;

                if (monthName.ToLower() == month)
                {
                    return i;
                }
                if (monthName.Substring(0, 3).ToLower() == month)
                {
                    return i;
                }
                ++i;
            }

            return -1;
        }

        private static bool IsMonth(string month)
        {
            return ParseMonth(month) != -1;
        }

        private static int ParseNumberOrMonth(string month)
        {
            int number;
            if (int.TryParse(month, out number))
            {
                return number;
            }

            return ParseMonth(month);
        }
    }
}
