using System;

namespace MusicDatabase
{
    public class ToggleButton : System.Windows.Controls.Primitives.ToggleButton
    {
        public ToggleButton()
        {
            this.Checked += new System.Windows.RoutedEventHandler(ToggleButton_Checked);
            this.Unchecked += new System.Windows.RoutedEventHandler(ToggleButton_Unchecked);
            this.Indeterminate += new System.Windows.RoutedEventHandler(ToggleButton_Indeterminate);
        }

        void ToggleButton_Indeterminate(object sender, System.Windows.RoutedEventArgs e)
        {
            this.OnCheckedChanged();
        }

        void ToggleButton_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.OnCheckedChanged();
        }

        private void ToggleButton_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            this.OnCheckedChanged();
        }

        public event EventHandler CheckedChanged;
        private void OnCheckedChanged()
        {
            if (this.CheckedChanged != null)
            {
                this.CheckedChanged(this, EventArgs.Empty);
            }
        }
    }
}
