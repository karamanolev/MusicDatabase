using System;
using System.Linq;
using System.Windows;
using MusicDatabase.Audio.Encoding;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for EncodingWindow.xaml
    /// </summary>
    public partial class EncodingWindow : MusicDatabaseWindow
    {
        public class EncodingItemView : DependencyObject
        {
            public string Status
            {
                get { return (string)GetValue(StatusProperty); }
                set { SetValue(StatusProperty, value); }
            }
            public static readonly DependencyProperty StatusProperty =
                DependencyProperty.Register("Status", typeof(string), typeof(EncodingItemView), new PropertyMetadata("Waiting"));


            public Visibility ProgressVisibility
            {
                get { return (Visibility)GetValue(ProgressVisibilityProperty); }
                set { SetValue(ProgressVisibilityProperty, value); }
            }
            public static readonly DependencyProperty ProgressVisibilityProperty =
                DependencyProperty.Register("ProgressVisibility", typeof(Visibility), typeof(EncodingItemView), new PropertyMetadata(Visibility.Hidden));


            public double Progress
            {
                get { return (double)GetValue(ProgressProperty); }
                set { SetValue(ProgressProperty, value); }
            }
            public static readonly DependencyProperty ProgressProperty =
                DependencyProperty.Register("Progress", typeof(double), typeof(EncodingItemView), new PropertyMetadata(0d));


            public string Target { get; private set; }
            public string Info { get; private set; }

            public EncodingItemView(string target, string info)
            {
                this.Target = target;
                this.Info = info;
            }
        }

        private EncodingItemView[] viewItems;
        private IEncoderController controller;
        private DateTime lastUIUpdate = DateTime.MinValue;

        public EncodingWindow(IEncoderController controller)
        {
            this.controller = controller;
            this.controller.Completed += this.controller_Completed;
            foreach (IParallelTask task in this.controller.Tasks)
            {
                task.ProgressChanged += this.task_ProgressChanged;
            }

            InitializeComponent();

            this.viewItems = this.controller.Tasks.Select(task => new EncodingItemView(task.Target, task.Info)).ToArray();
            this.listTasks.ItemsSource = this.viewItems;

            controller.Start();
        }

        void task_ProgressChanged(object sender, EventArgs e)
        {
            if (DateTime.Now.Subtract(this.lastUIUpdate) >= ProgressBarUpdater.UpdateInterval)
            {
                this.lastUIUpdate = DateTime.Now;
                this.Dispatcher.BeginInvokeAction(() =>
                {
                    this.UpdateUI();
                });
            }
        }

        private void controller_Completed(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.UpdateUI();

                this.textStatus.Text = this.controller.Status.ToString();
                if (this.controller.Status == EncoderControllerStatus.Completed)
                {
                    this.DialogResult = true;
                }
                else if (this.controller.Status == EncoderControllerStatus.Completed ||
                    this.controller.Status == EncoderControllerStatus.Cancelled ||
                    this.controller.Status == EncoderControllerStatus.Faulted)
                {
                    this.btnCancel.Content = "Close";
                }
            }));
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.controller.Completed -= this.controller_Completed;
            foreach (IParallelTask task in this.controller.Tasks)
            {
                task.ProgressChanged -= this.task_ProgressChanged;
            }
        }

        private void UpdateUI()
        {
            this.textStatus.Text = "Encoding";
            this.progressBar.Value = this.controller.Tasks.Select(t => t.Progress).Average();

            for (int i = 0; i < this.controller.Tasks.Length; ++i)
            {
                EncodingItemView view = this.viewItems[i];
                IParallelTask task = this.controller.Tasks[i];

                string statusString = task.Status == EncodeTaskStatus.Processing ? "" : task.Status.ToString();
                if (checkScrollLock.IsChecked != true && view.Status != statusString)
                {
                    this.listTasks.ScrollIntoView(view);
                }

                view.Progress = this.controller.Tasks[i].Progress;
                view.ProgressVisibility = this.controller.Tasks[i].Status == EncodeTaskStatus.Processing ? Visibility.Visible : Visibility.Collapsed;
                view.Status = statusString;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (this.controller.Status == EncoderControllerStatus.Running)
            {
                if (Dialogs.Confirm())
                {
                    if (this.controller.Status == EncoderControllerStatus.Running)
                    {
                        this.controller.Cancel();
                    }
                }
            }
            else
            {
                this.Close();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.controller.Status == EncoderControllerStatus.Running)
            {
                if (Dialogs.Confirm())
                {
                    if (this.controller.Status == EncoderControllerStatus.Running)
                    {
                        this.controller.Cancel();
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
