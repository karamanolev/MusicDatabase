using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase
{
    public partial class AdditionalFileEditor : UserControl
    {
        public ReleaseAdditionalFileType FileType
        {
            get { return (ReleaseAdditionalFileType)this.comboType.SelectedItem; }
            set { this.comboType.SelectedItem = value; }
        }

        public ReleaseAdditionalFile AssociatedFile { get; set; }

        public string FileName
        {
            get { return this.fileBox.FileName; }
        }

        public byte[] FileBytes
        {
            get { return this.fileBox.FileBytes; }
        }

        public string FileDescription
        {
            get { return this.textDescription.Text; }
            set { this.textDescription.Text = value; }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(AdditionalFileEditor), new UIPropertyMetadata(false, OnIsReadOnlyPropertyChangedCallback));
        private static void OnIsReadOnlyPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            AdditionalFileEditor editor = (AdditionalFileEditor)sender;
            editor.btnDelete.Visibility = editor.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
            editor.comboType.IsEnabled = !editor.IsReadOnly;

            editor.labelDescription.Visibility = editor.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
            editor.textDescription.Visibility = editor.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
        }

        public AdditionalFileEditor()
        {
            this.DataContext = this;

            InitializeComponent();

            this.comboType.ItemsSource = Enum.GetValues(typeof(ReleaseAdditionalFileType));
        }

        public void SetFile(string fileName, byte[] fileBytes)
        {
            this.fileBox.SetFile(fileName, fileBytes);
        }

        private void btnDelete_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Dialogs.Confirm())
            {
                this.OnDeleteClicked();
            }
        }

        private void textDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.OnDataChanged();
        }

        private void comboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.OnDataChanged();
        }

        public event EventHandler DeleteClicked;
        private void OnDeleteClicked()
        {
            if (this.DeleteClicked != null)
            {
                this.DeleteClicked(this, EventArgs.Empty);
            }
        }

        private void btnSave_Clicked(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = Path.GetFileName(this.AssociatedFile.OriginalFilename);
            dialog.Filter = ReleaseAdditionalFile.GetFilterForType(this.AssociatedFile.Type);
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllBytes(dialog.FileName, this.FileBytes);
            }
        }

        public event EventHandler DataChanged;
        private void OnDataChanged()
        {
            if (this.DataChanged != null)
            {
                this.DataChanged(this, EventArgs.Empty);
            }
        }

        private void btnView_Clicked(object sender, RoutedEventArgs e)
        {
            ViewAdditionalFileWindow viewAdditionalFileWindow = new ViewAdditionalFileWindow();
            viewAdditionalFileWindow.AdditionalFile = this.AssociatedFile;
            viewAdditionalFileWindow.ShowDialog(Window.GetWindow(this));
        }
    }
}
