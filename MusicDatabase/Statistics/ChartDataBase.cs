using System;
using System.Collections;
using System.Linq;
using System.Windows.Controls.DataVisualization.Charting;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Statistics
{
    abstract class ChartDataBase
    {
        public DataPointSeries Series { get; private set; }
        public abstract bool HasFilter { get; }

        public ChartDataBase(DataPointSeries series)
        {
            this.Series = series;
        }

        public abstract IEnumerable GetDistribution(Release[] releases);
        public abstract Tuple<string, Func<Release, bool>> GetFilter(object selection);
    }
}
