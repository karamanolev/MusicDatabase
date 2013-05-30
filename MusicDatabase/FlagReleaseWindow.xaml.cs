using System;
using System.Windows;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for FlagReleaseWindow.xaml
    /// </summary>
    public partial class FlagReleaseWindow : Window
    {
        public string FlagMessage
        {
            get
            {
                return this.textMessage.Text;
            }
            set
            {
                this.textMessage.Text = value;
            }
        }

        public FlagReleaseWindow()
        {
            InitializeComponent();

            this.textMessage.Focus();
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
