using System;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for VerificationWindow.xaml
    /// </summary>
    public partial class VerificationWindow : MusicDatabaseWindow
    {
        private static Random random = new Random();
        private int code;

        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public VerificationWindow(string warningText)
        {
            InitializeComponent();

            this.labelWarning.Text = warningText;
            this.code = random.Next(1000, 10000);
            this.labelCode.Text = "Enter " + this.code + " to continue: ";

            this.textCode.Focus();
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            this.DialogResult = true;
        }

        private void textCode_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.okCancelBox.IsOKEnabled = this.textCode.Text == this.code.ToString();
        }
    }
}
