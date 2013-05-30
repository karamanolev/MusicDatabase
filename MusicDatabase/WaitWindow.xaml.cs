using System;
using System.Windows;
using System.Threading.Tasks;
using MusicDatabase.Engine;

namespace MusicDatabase
{
    public partial class WaitWindow : Window
    {
        public bool CanClose { get; set; }

        public WaitWindow(string message)
        {
            InitializeComponent();

            this.labelMessage.Text = message;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = !this.CanClose;
        }

        private async void DoBackgroundWork(Task action, Progress<double> progress)
        {
            if (progress != null)
            {
                progress.ProgressChanged += progress_ProgressChanged;
                this.progressBar.IsIndeterminate = false;
            }
            else
            {
                this.progressBar.IsIndeterminate = true;
            }

            try
            {
                action.Start();
                await action;
            }
            catch (Exception ex)
            {
                Utility.WriteToErrorLog(ex.ToString());
                Dialogs.Error("Error during asynchronous operation (see error log).");
            }
            finally
            {
                if (progress != null)
                {
                    progress.ProgressChanged -= progress_ProgressChanged;
                }
            }

            this.CanClose = true;
            this.Close();
        }

        void progress_ProgressChanged(object sender, double e)
        {
            this.progressBar.Value = e;
        }

        public void ShowDialog(Window owner, Action action)
        {
            this.ShowDialog(owner, new Task(action));
        }

        public void ShowDialog(Window owner, Task action, Progress<double> progress = null)
        {
            this.Owner = owner;
            this.DoBackgroundWork(action, progress);
            this.ShowDialog();
        }
    }
}
