using System;
using System.Linq;
using CsQuery;
using MusicDatabase.Engine.Entities;
using System.Text.RegularExpressions;
using System.Globalization;

namespace MusicDatabase.Engine
{
    class WikipediaAlbumParseResult
    {
        public string PageTitle { get; private set; }
        public ReleaseDate ReleaseDate { get; private set; }

        public static WikipediaAlbumParseResult Parse(string pageUrl)
        {
            WikipediaAlbumParseResult result = new WikipediaAlbumParseResult();

            var dom = CQ.CreateFromUrl(pageUrl);

            var title = dom.Select("h1.firstHeading").Text();
            if (string.IsNullOrEmpty(title))
            {
                return null;
            }
            result.PageTitle = title;

            var published = dom.Select(".published").Text();

            result.ReleaseDate = ParseWikipediaReleaseDate(published);

            return result;
        }

        private static ReleaseDate ParseWikipediaReleaseDate(string published)
        {
            string[] parts = Regex.Split(published, "[^0-9a-zA-Z");

            if (parts.Length == 1)
            {
                int year;
                if (int.TryParse(parts[0], out year))
                {
                    return new ReleaseDate(year);
                }
            }
            else if (parts.Length == 2)
            {
                int part1 = ParseNumberOrMonth(parts[0]);
                int part2 = ParseNumberOrMonth(parts[1]);
                if (part1 > 50)
                {
                    return new ReleaseDate(part1, part2);
                }
                else
                {
                    return new ReleaseDate(part2, part1);
                }
            }

            for (int i = 0; i < parts.Length - 2; ++i)
            {
                ReleaseDate date = ParseReleaseDateTriplet(new string[] { parts[i], parts[i + 1], parts[i + 1] });
                if (date.IsValid)
                {
                    return date;
                }
            }

            return new ReleaseDate();
        }

        private static ReleaseDate ParseReleaseDateTriplet(string[] triplet)
        {
            ReleaseDate releaseDate = new ReleaseDate();

            int part0 = ParseNumberOrMonth(triplet[0]);
            int part1 = ParseNumberOrMonth(triplet[1]);
            int part2 = ParseNumberOrMonth(triplet[2]);

            if (part0 > 50)
            {
                if (IsMonth(triplet[1]))
                {
                    return new ReleaseDate(part0, part1, part2);
                }
                else if (IsMonth(triplet[2]))
                {
                    return new ReleaseDate(part0, part2, part1);
                }
                return new ReleaseDate();
            }
            else if (part2 > 50)
            {
                if (IsMonth(triplet[0]))
                {
                    return new ReleaseDate(part2, part0, part1);
                }
                else if (IsMonth(triplet[1]))
                {
                    return new ReleaseDate(part2, part1, part0);
                }
            }

            return new ReleaseDate();
        }

        private static bool IsMonth(string month)
        {
            return DateTimeFormatInfo.InvariantInfo.MonthNames.Select(s => s.ToLower()).Contains(month.ToLower());
        }

        private static int ParseNumberOrMonth(string month)
        {
            int number;
            if (int.TryParse(month, out number))
            {
                return number;
            }

            month = month.ToLower();

            int i = 1;
            foreach (string monthName in DateTimeFormatInfo.InvariantInfo.MonthNames)
            {
                if (monthName.ToLower() == month)
                {
                    return i;
                }
                ++i;
            }

            return -1;
        }
    }
}
