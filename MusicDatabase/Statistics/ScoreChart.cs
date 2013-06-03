using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls.DataVisualization.Charting;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Statistics
{
    class ScoreChart : ChartDataBase
    {
        private const int BucketSize = 10;

        public class Item
        {
            public string Key { get; set; }
            public int Value { get; set; }

            public int Start { get; set; }
            public int End { get; set; }
        }

        public override bool HasFilter
        {
            get { return true; }
        }

        public ScoreChart(DataPointSeries series)
            : base(series)
        {
        }

        public override IEnumerable GetDistribution(Release[] releases)
        {
            if (releases.Length == 0)
            {
                yield break;
            }

            List<int> buckets = new List<int>(Enumerable.Repeat(0, BucketSize + 1));

            foreach (Release release in releases)
            {
                ++buckets[release.Score / BucketSize];
            }

            bool started = false;

            int bucketIndex = 0;
            foreach (int bucket in buckets)
            {
                started = started || (bucket > 0);

                if (started)
                {
                    int bucketRangeStart = bucketIndex * BucketSize;
                    int bucketRangeEnd = bucketRangeStart + BucketSize - 1;
                    string key = bucketIndex == 10 ? "100" : (bucketRangeStart + " - " + bucketRangeEnd);

                    yield return new Item()
                    {
                        Key = key,
                        Value = bucket,
                        Start = bucketRangeStart,
                        End = bucketRangeEnd
                    };
                }

                ++bucketIndex;
            }
        }

        public override Tuple<string, Func<Release, bool>> GetFilter(object selection)
        {
            Item item = (Item)selection;
            return new Tuple<string, Func<Release, bool>>("Score = " + item.Key, r =>
            {
                return item.Start <= r.Score && r.Score <= item.End;
            });
        }
    }
}
