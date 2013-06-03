using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.DataVisualization.Charting;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Statistics
{
    class CountryChart : ChartDataBase
    {
        public class Item
        {
            public string Key { get; set; }
            public int Value { get; set; }

            public string[] Countries { get; set; }
        }

        public override bool HasFilter
        {
            get { return false; }
        }

        public CountryChart(DataPointSeries series)
            : base(series)
        {
        }

        public override IEnumerable GetDistribution(Release[] releases)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (Release release in releases)
            {
                if (!string.IsNullOrEmpty(release.Country))
                {
                    if (!result.ContainsKey(release.Country))
                    {
                        result[release.Country] = 0;
                    }

                    ++result[release.Country];
                }
            }

            var allItems = result.OrderBy(i => -i.Value);
            var topItems = allItems.Take(6);
            var otherItems = allItems.Skip(6);
            int otherItemsSum = otherItems.Sum(i => i.Value);

            foreach (var topItem in topItems)
            {
                yield return new Item()
                {
                    Key = topItem.Key + " (" + topItem.Value + ")",
                    Value = topItem.Value,
                    Countries = new string[] { topItem.Key },
                };
            }

            if (otherItemsSum != 0)
            {
                yield return new Item()
                {
                    Key = "Other (" + otherItemsSum + ")",
                    Value = otherItemsSum,
                    Countries = otherItems.Select(i => i.Key).ToArray()
                };
            }
        }

        public override Tuple<string, Func<Release, bool>> GetFilter(object selection)
        {
            Item item = (Item)selection;
            return new Tuple<string, Func<Release, bool>>("Country = " + item.Key, r => item.Countries.Contains(r.Country));
        }
    }
}
