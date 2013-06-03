using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Statistics
{
    /// <summary>
    /// Interaction logic for CollectionStatisticsControl.xaml
    /// </summary>
    public partial class CollectionStatisticsControl : UserControl
    {
        private MainCollectionView mainCollectionView;
        private Release[] releases;
        private Task updateUITask;
        private List<ChartDataBase> charts;

        public Release[] Releases
        {
            get { return this.releases; }
            set
            {
                if (!object.ReferenceEquals(value, this.releases))
                {
                    this.releases = value;
                    this.UpdateUI();
                }
            }
        }

        public CollectionStatisticsControl()
        {
            InitializeComponent();

            this.charts = new List<ChartDataBase>()
            {
                new YearReleasedChart(this.barYearDistribution),
                new GenreChart(this.pieGenreDistribution),
                new CountryChart(this.pieCountryDistribution),
                new ImportDateChart(this.lineImportDateDistribution),
                new ScoreChart(this.barScoreDistribution),
                new DynamicRangeChart(this.barDynamicRangeDistribution),
                new ReplayGainChart(this.barReplayGainDistribution),
            };

            foreach (var item in this.charts)
            {
                if (item.HasFilter)
                {
                    item.Series.SelectionChanged += this.chart_SelectionChanged;
                    item.Series.IsSelectionEnabled = true;
                }
            }

            this.updateUITask = new Task(() => { });
            this.updateUITask.Start();
        }

        public void Init(MainCollectionView mainCollectionView)
        {
            this.mainCollectionView = mainCollectionView;
        }

        private void chart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChartDataBase chartData = null;

            DataPointSeries seriesSender = (DataPointSeries)sender;
            foreach (var item in this.charts)
            {
                if (item.Series == sender)
                {
                    chartData = item;
                    continue;
                }

                item.Series.SelectionChanged -= this.chart_SelectionChanged;
                item.Series.SelectedItem = null;
                item.Series.SelectionChanged += this.chart_SelectionChanged;
            }

            if (!chartData.HasFilter)
            {
                this.mainCollectionView.ClearSearch();
            }
            else
            {
                var filter = chartData.GetFilter(seriesSender.SelectedItem);
                this.mainCollectionView.SpecialSearch(filter.Item1, filter.Item2);
            }
        }

        private void UpdateUIWorker()
        {
            foreach (var item in this.charts)
            {
                this.FillDataAsync(item.Series, item.GetDistribution);
            }
        }

        private void FillDataAsync(DataPointSeries target, Func<Release[], IEnumerable> source)
        {
            var items = source(this.releases);
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
    }
}
