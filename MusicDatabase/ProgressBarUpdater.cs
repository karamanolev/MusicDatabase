using System;
using System.Windows.Controls;
using System.Windows.Threading;
using MusicDatabase.Engine;
using System.Windows;
using Telerik.Windows.Controls;

namespace MusicDatabase
{
    public class ProgressBarUpdater
    {
        public static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(50);

        public static EventHandler<ProgressChangedEventArgs> CreateHandler(Dispatcher dispatcher, FrameworkElement progressBar)
        {
            return CreateHandler(dispatcher, progressBar, () => false);
        }

        public static EventHandler<ProgressChangedEventArgs> CreateHandler(Dispatcher dispatcher, FrameworkElement progressBar, Func<bool> cancelled)
        {
            return CreateHandler(dispatcher, progressBar, cancelled, p => { });
        }

        public static EventHandler<ProgressChangedEventArgs> CreateHandler(Dispatcher dispatcher, FrameworkElement progressBar, Func<bool> cancelled, Action<double> onDispatcherInvoke)
        {
            DateTime lastUpdate = DateTime.MinValue;
            return new EventHandler<ProgressChangedEventArgs>((sender, e) =>
            {
                if (DateTime.Now.Subtract(lastUpdate) < UpdateInterval && e.Progress != 1)
                {
                    return;
                }
                lastUpdate = DateTime.Now;

                dispatcher.Invoke(new Action(() =>
                {
                    UpdateProgressValue(progressBar, e.Progress);
                    onDispatcherInvoke(e.Progress);
                }));

                e.Cancel = cancelled();
            });
        }

        private static void UpdateProgressValue(FrameworkElement progressBar, double value)
        {
            if (progressBar is ProgressBar)
            {
                ((ProgressBar)progressBar).Value = value;
            }
            else if (progressBar is RadBusyIndicator)
            {
                ((RadBusyIndicator)progressBar).ProgressValue = (int)(value * 100);
                ((RadBusyIndicator)progressBar).BusyContent = (int)(value * 100) + "%";
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
