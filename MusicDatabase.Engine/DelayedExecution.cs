using System;
using System.Threading;

namespace MusicDatabase.Engine
{
    public class DelayedExecution
    {
        private TimeSpan TimeSpanNegative = TimeSpan.FromMilliseconds(-1);
        private Action method;
        private TimeSpan delay;
        private Timer timer;

        public DelayedExecution(Action method, TimeSpan delay)
        {
            this.method = method;
            this.delay = delay;
            this.timer = new Timer(this.Callback);
        }

        public TimeSpan Delay
        {
            get { return this.delay; }
            set
            {
                this.delay = value;
            }
        }

        public void Enable()
        {
            this.timer.Change(this.delay, TimeSpanNegative);
        }

        public void Disable()
        {
            this.timer.Change(TimeSpanNegative, TimeSpanNegative);
        }

        private void Callback(object state)
        {
            this.method();
        }
    }
}
