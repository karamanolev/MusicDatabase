using System;
using System.Windows;
using System.Windows.Controls;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for OKCancelBox.xaml
    /// </summary>
    public partial class OKCancelBox : UserControl
    {
        public bool IsOKEnabled
        {
            get { return (bool)GetValue(IsOKEnabledProperty); }
            set { SetValue(IsOKEnabledProperty, value); }
        }
        public static readonly DependencyProperty IsOKEnabledProperty =
            DependencyProperty.Register("IsOKEnabled", typeof(bool), typeof(OKCancelBox), new PropertyMetadata(true));

        public OKCancelBox()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.OnOKClicked();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.OnCancelClicked();
        }

        public event EventHandler OKClicked;
        private void OnOKClicked()
        {
            if (this.OKClicked != null)
            {
                this.OKClicked(this, EventArgs.Empty);
            }
        }

        public event EventHandler CancelClicked;
        private void OnCancelClicked()
        {
            if (this.CancelClicked != null)
            {
                this.CancelClicked(this, EventArgs.Empty);
            }
        }
    }
}
