using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MusicDatabase.Engine.Entities;
using System.Windows.Controls.DataVisualization.Charting;

namespace MusicDatabase.Statistics
{
    class ReplayGainChart : ChartDataBase
    {
        public override bool HasFilter
        {
            get { return true; }
        }

        public ReplayGainChart(DataPointSeries series)
            : base(series)
        {
        }

        public override IEnumerable GetDistribution(Release[] releases)
        {
            SortedDictionary<int, int> result = new SortedDictionary<int, int>();
            if (releases.Length == 0)
            {
                return result;
            }

            var releasesWithRg = releases.Where(r => !double.IsNaN(r.ReplayGainAlbumGain)).ToArray();
            int minRg = (int)releasesWithRg.Min(r => r.ReplayGainAlbumGain);
            int maxRg = (int)releasesWithRg.Max(r => r.ReplayGainAlbumGain);
            for (int i = minRg; i <= maxRg; ++i)
            {
                result[i] = 0;
            }
            foreach (Release release in releasesWithRg)
            {
                ++result[(int)release.ReplayGainAlbumGain];
            }
            return result;
        }

        public override Tuple<string, Func<Release, bool>> GetFilter(object selection)
        {
            KeyValuePair<int, int> item = (KeyValuePair<int, int>)selection;
            return new Tuple<string, Func<Release, bool>>("ReplayGain = " + item.Key, r => (int)r.ReplayGainAlbumGain == item.Key);
        }
    }
}
