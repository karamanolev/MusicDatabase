using System;

namespace MusicDatabase.Engine
{
    public class ProgressChangedEventArgs : EventArgs
    {
        public double Progress { get; private set; }
        public bool Cancel { get; set; }

        public ProgressChangedEventArgs(double progress)
        {
            this.Progress = progress;
        }
    }
}
