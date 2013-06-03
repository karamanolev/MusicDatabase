using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.DataVisualization.Charting;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Statistics
{
    class YearReleasedChart : ChartDataBase
    {
        public class Item
        {
            public string Key { get; set; }
            public int Value { get; set; }

            public int Start { get; set; }
            public int End { get; set; }
        }

        private const int Step = 5;

        public override bool HasFilter
        {
            get { return true; }
        }

        public YearReleasedChart(DataPointSeries series)
            : base(series)
        {
        }

        private int GetYear(Release release)
        {
            ReleaseDate releaseDate = release.OriginalReleaseDate != null && release.OriginalReleaseDate.IsValid ? release.OriginalReleaseDate : release.ReleaseDate;
            if (releaseDate.Type != ReleaseDateType.Invalid)
            {
                return releaseDate.Date.Year;
            }
            return 0;
        }

        public override IEnumerable GetDistribution(Release[] releases)
        {
            SortedDictionary<int, int> result = new SortedDictionary<int, int>();
            foreach (Release release in releases)
            {
                int year = this.GetYear(release);
                if (year != 0)
                {
                    int bucket = year / Step * Step;

                    if (!result.ContainsKey(bucket))
                    {
                        result[bucket] = 0;
                    }

                    ++result[bucket];
                }
            }

            foreach (var item in result)
            {
                int startYear = item.Key;
                int endYear = item.Key + Step - 1;
                yield return new Item()
                {
                    Key = " " + startYear + "-" + endYear + " ",
                    Value = item.Value,
                    Start = startYear,
                    End = endYear
                };
            }
        }

        public override Tuple<string, Func<Release, bool>> GetFilter(object selection)
        {
            Item item = (Item)selection;
            return new Tuple<string, Func<Release, bool>>("Year = " + item.Key, r =>
            {
                int year = this.GetYear(r);
                return item.Start <= year && year <= item.End;
            });
        }
    }
}
