using System;
using System.Collections;
using System.Linq;
using System.Windows.Controls.DataVisualization.Charting;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Statistics
{
    class ImportDateChart : ChartDataBase
    {
        public class Item
        {
            public string Key { get; set; }
            public int Value { get; set; }

            public DateTime Start { get; set; }
            public DateTime End { get; set; }
        }

        public override bool HasFilter
        {
            get { return true; }
        }

        public ImportDateChart(DataPointSeries series)
            : base(series)
        {
        }

        private int MonthsDifference(DateTime larger, DateTime smaller)
        {
            return ((larger.Year - smaller.Year) * 12) + larger.Month - smaller.Month;
        }

        public override IEnumerable GetDistribution(Release[] releases)
        {
            if (releases.Length == 0)
            {
                yield break;
            }

            DateTime[] dates = releases.Select(r => r.DateAdded).ToArray();

            DateTime start = dates.Min();
            start = new DateTime(start.Year, start.Month, 1);

            int[] offsets = dates.Select(d => this.MonthsDifference(d, start)).ToArray();
            int maxOffset = offsets.Max();
            int scale = maxOffset / 32 + 1;
            maxOffset /= scale;

            int[] counts = new int[maxOffset + 1];
            for (int i = 0; i < offsets.Length; ++i)
            {
                int limit = offsets[i] / scale;
                for (int j = limit; j < counts.Length; ++j)
                {
                    ++counts[j];
                }
            }

            for (int i = 0; i < counts.Length; ++i)
            {
                int offset = i * scale;
                DateTime date1 = start.AddMonths(offset);
                DateTime date2 = start.AddMonths(offset + scale - 1);
                string label;
                if (date1.Equals(date2))
                {
                    label = date1.ToString("MM/yy");
                }
                else
                {
                    label = date1.Month.ToString("00") + "-" + date2.Month.ToString("00") + "/" + date1.ToString("yy");
                }

                yield return new Item()
                {
                    Key = " " + label + " ",
                    Value = counts[i],
                    Start = start.AddMonths(offset),
                    End = start.AddMonths(offset + scale)
                };
            }
        }

        public override Tuple<string, Func<Release, bool>> GetFilter(object selection)
        {
            Item item = (Item)selection;
            return new Tuple<string, Func<Release, bool>>("Date Added = " + item.Key, r =>
            {
                return item.Start <= r.DateAdded && r.DateAdded < item.End;
            });
        }
    }
}
