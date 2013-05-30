using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for CollectionStatisticsControl.xaml
    /// </summary>
    public partial class CollectionStatisticsControl : UserControl
    {
        private const int Step = 5;

        private Release[] releases;
        private Task updateUITask;

        public CollectionManager CollectionManager
        {
            set
            {
                this.releases = value.Releases.ToArray();
            }
        }

        public CollectionStatisticsControl()
        {
            InitializeComponent();

            this.updateUITask = new Task(() => { });
            this.updateUITask.Start();
        }

        private void UpdateUIWorker()
        {
            this.FillDataAsync(this.barYearDistribution, this.GetYearDistribution);
            this.FillDataAsync(this.pieGenreDistribution, this.GetGenreDistribution);
            this.FillDataAsync(this.pieCountryDistribution, this.GetCountryDistribution);
            this.FillDataAsync(this.barImportYearDistribution, this.GetImportMonthDistribution);
            this.FillDataAsync(this.columnScoreDistribution, this.GetScoreDistribution);
        }

        private void FillDataAsync(DataPointSeries target, Func<IEnumerable> source)
        {
            var items = source();
            this.Dispatcher.BeginInvokeAction(() =>
            {
                try
                {
                    target.ItemsSource = items;
                }
                catch
                {
                }
            });
        }

        public void UpdateUI()
        {
            this.updateUITask.Wait();

            if (this.releases != null)
            {
                this.updateUITask = new Task(this.UpdateUIWorker);
                this.updateUITask.Start();
            }
        }

        private IEnumerable GetYearDistribution()
        {
            SortedDictionary<string, int> result = new SortedDictionary<string, int>();
            foreach (Release release in this.releases)
            {
                ReleaseDate releaseDate = release.OriginalReleaseDate != null && release.OriginalReleaseDate.IsValid ? release.OriginalReleaseDate : release.ReleaseDate;
                if (releaseDate.Type != ReleaseDateType.Invalid)
                {
                    int year = releaseDate.Date.Year / Step;
                    string yearString = (year * Step) + "-" + (year * Step + Step - 1);

                    if (!result.ContainsKey(yearString))
                    {
                        result[yearString] = 0;
                    }

                    ++result[yearString];
                }
            }
            return result;
        }

        private IEnumerable GetGenreDistribution()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (Release release in this.releases)
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

            var allItems = result.OrderBy(i => -i.Value);
            var topItems = allItems.Take(6);
            int otherItems = allItems.Skip(6).Sum(i => i.Value);

            foreach (var topItem in topItems)
            {
                yield return new KeyValuePair<string, int>(topItem.Key + " (" + topItem.Value + ")", topItem.Value);
            }

            if (otherItems != 0)
            {
                yield return new KeyValuePair<string, int>("Other (" + otherItems + ")", otherItems);
            }
        }

        private IEnumerable GetCountryDistribution()
        {
            Dictionary<string, int> result = new Dictionary<string, int>();

            foreach (Release release in this.releases)
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
            int otherItems = allItems.Skip(6).Sum(i => i.Value);

            foreach (var topItem in topItems)
            {
                yield return new KeyValuePair<string, int>(topItem.Key + " (" + topItem.Value + ")", topItem.Value);
            }

            if (otherItems != 0)
            {
                yield return new KeyValuePair<string, int>("Other (" + otherItems + ")", otherItems);
            }
        }

        private DateTime GetYearMonth(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }

        private IEnumerable GetImportMonthDistribution()
        {
            if (releases.Length == 0)
            {
                yield break;
            }

            DateTime start = this.GetYearMonth(releases.Select(r => r.DateAdded).Min());
            DateTime end = this.GetYearMonth(releases.Select(r => r.DateAdded).Max());

            SortedDictionary<DateTime, int> result = new SortedDictionary<DateTime, int>();

            while (start <= end)
            {
                result[start] = 0;
                start = start.AddMonths(1);
            }

            foreach (Release release in releases)
            {
                if (release.DateAdded != null)
                {
                    DateTime importKey = new DateTime(release.DateAdded.Year, release.DateAdded.Month, 1);
                    foreach (var item in result.ToArray())
                    {
                        if (item.Key >= importKey)
                        {
                            ++result[item.Key];
                        }
                    }
                }
            }

            foreach (var item in result)
            {
                yield return new KeyValuePair<string, int>(item.Key.ToString("MM/yy"), item.Value);
            }
        }

        private IEnumerable GetScoreDistribution()
        {
            if (this.releases.Length == 0)
            {
                yield break;
            }

            List<int> buckets = new List<int>(Enumerable.Repeat(0, 11));

            foreach (Release release in releases)
            {
                ++buckets[release.Score / 10];
            }

            bool started = false;
            int bucketIndex = 0;
            foreach (int bucket in buckets)
            {
                started = started || (bucket > 0);

                if (started)
                {
                    int bucketRangeStart = bucketIndex * 10;
                    string key = bucketIndex == 11 ? "100" : (bucketRangeStart + " - " + (bucketRangeStart + 9));
                    yield return new KeyValuePair<string, int>(key, bucket);
                }

                ++bucketIndex;
            }
        }
    }
}
