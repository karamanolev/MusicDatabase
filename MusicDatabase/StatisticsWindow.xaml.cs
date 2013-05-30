using System;
using System.Linq;
using MusicDatabase.Engine;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for StatisticsWindow.xaml
    /// </summary>
    public partial class StatisticsWindow : MusicDatabaseWindow
    {
        public class StatisticsListViewItem
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }

        private bool shouldCancel;
        private bool canClose;
        private CollectionStatisticsGenerator generator;
        private CollectionStatistics collectionStatistics;
        private Task task;

        public StatisticsWindow(ICollectionSessionFactory collectionSessionFactory)
            : base(collectionSessionFactory)
        {
            InitializeComponent();

            this.generator = new CollectionStatisticsGenerator(this.CollectionManager);
            this.generator.ProgressChanged += ProgressBarUpdater.CreateHandler(this.Dispatcher, this.busyIndicator, () => this.shouldCancel);
        }

        private void UpdateUI()
        {
            List<StatisticsListViewItem> items = new List<StatisticsListViewItem>();

            foreach (PropertyInfo property in typeof(CollectionStatistics).GetProperties())
            {
                string humanName = Utility.PascalCaseToString(property.Name);
                object value = property.GetValue(this.collectionStatistics, null);

                if (property.Name.Contains("Bytes"))
                {
                    long doubleValue = Convert.ToInt64(value);
                    value = Utility.BytesToString(doubleValue);
                }

                items.Add(new StatisticsListViewItem()
                {
                    Name = humanName,
                    Value = value
                });
            }

            this.listView.ItemsSource = items;
        }

        private void MusicDatabaseWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.task = new Task(() => {
                try
                {
                    this.collectionStatistics = this.generator.ComputeStatistics();

                    if (this.collectionStatistics != null)
                    {
                        this.Dispatcher.BeginInvokeAction(this.UpdateUI);
                    }
                }
                catch (Exception ex)
                {
                    Utility.WriteToErrorLog(ex.ToString());
                    Dialogs.Error("Error calculating statistics: " + ex.Message);
                }
                finally
                {
                    this.canClose = true;
                    this.Dispatcher.BeginInvokeAction(() => this.busyIndicator.IsBusy = false);
                    if (this.shouldCancel)
                    {
                        this.Dispatcher.BeginInvokeAction(this.Close);
                    }
                }
            });
            this.task.Start();
        }

        private void btnClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Close();
        }

        private void MusicDatabaseWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.canClose)
            {
                this.shouldCancel = true;
                e.Cancel = true;
                return;
            }
        }
    }
}
