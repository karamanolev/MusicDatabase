using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ImagePicker.xaml
    /// </summary>
    public partial class ImagePicker : Window
    {
        public object SelectedItem
        {
            get
            {
                return this.listView.SelectedItem;
            }
        }

        public bool IsSkipped { get; private set; }

        public ImagePicker(string title, IEnumerable<object> items)
        {
            InitializeComponent();

            this.textTitle.Text = title;
            this.listView.ItemsSource = items;
        }

        private void OKCancelBox_OKClicked(object sender, EventArgs e)
        {
            if (this.SelectedItem == null)
            {
                Dialogs.Error("Please select an image.");
                return;
            }

            this.DialogResult = true;
            this.Close();
        }

        private void OKCancelBox_CancelClicked(object sender, EventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (this.SelectedItem != null)
                {
                    this.DialogResult = true;
                    this.Close();
                }
            }
        }

        private void btnSkip_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.IsSkipped = true;
        }
    }
}
