using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.DataVisualization.Charting;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Statistics
{
    class GenreChart : ChartDataBase
    {
        public class Item
        {
            public string Key { get; set; }
            public int Value { get; set; }

            public string[] Genres { get; set; }
        }

        public override bool HasFilter
        {
            get { return true; }
        }

        public GenreChart(DataPointSeries series)
            : base(series)
        {
        }

        public override IEnumerable GetDistribution(Release[] releases)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (Release release in releases)
            {
                if (!string.IsNullOrEmpty(release.Genre))
                {
                    if (!result.ContainsKey(release.Genre))
                    {
                        result[release.Genre] = 0;
                    }

                    ++result[release.Genre];
                }
            }

            var allItems = result.OrderByDescending(i => i.Value);
            var topItems = allItems.Take(6);
            var otherItems = allItems.Skip(6);
            int otherItemsSum = otherItems.Sum(i => i.Value);

            foreach (var topItem in topItems)
            {
                yield return new Item()
                {
                    Key = topItem.Key + " (" + topItem.Value + ")",
                    Value = topItem.Value,
                    Genres = new string[] { topItem.Key },
                };
            }

            if (otherItemsSum != 0)
            {
                yield return new Item()
                {
                    Key = "Other (" + otherItemsSum + ")",
                    Value = otherItemsSum,
                    Genres = otherItems.Select(i => i.Key).ToArray()
                };
            }
        }

        public override Tuple<string, Func<Release, bool>> GetFilter(object selection)
        {
            Item item = (Item)selection;
            return new Tuple<string, Func<Release, bool>>("Genre = " + item.Key, r => item.Genres.Contains(r.Genre));
        }
    }
}
