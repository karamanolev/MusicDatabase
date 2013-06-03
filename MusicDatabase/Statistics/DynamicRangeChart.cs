using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MusicDatabase.Engine.Entities;
using System.Windows.Controls.DataVisualization.Charting;

namespace MusicDatabase.Statistics
{
    class DynamicRangeChart : ChartDataBase
    {
        public override bool HasFilter
        {
            get { return true; }
        }

        public DynamicRangeChart(DataPointSeries series)
            : base(series)
        {
        }

        public override IEnumerable GetDistribution(Release[] releases)
        {
            SortedDictionary<int, int> result = new SortedDictionary<int, int>();

            var releasesWithDr = releases.Where(r => r.DynamicRange != 0).ToArray();
            if (releasesWithDr.Length == 0)
            {
                return result;
            }

            int minDr = (int)Math.Round(releasesWithDr.Min(r => r.DynamicRange));
            int maxDr = (int)Math.Round(releasesWithDr.Max(r => r.DynamicRange));
            for (int i = minDr; i <= maxDr; ++i)
            {
                result[i] = 0;
            }

            foreach (Release release in releasesWithDr)
            {
                ++result[(int)Math.Round(release.DynamicRange)];
            }

            return result;
        }

        public override Tuple<string, Func<Release, bool>> GetFilter(object selection)
        {
            KeyValuePair<int, int> item = (KeyValuePair<int, int>)selection;
            return new Tuple<string, Func<Release, bool>>("Dynamic Range = " + item.Key, r => Math.Round(r.DynamicRange) == item.Key);
        }
    }
}
