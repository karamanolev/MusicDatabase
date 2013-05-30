using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Win32;
using MusicDatabase.Engine;
using NImageMagick;

namespace MusicDatabase.ImageStudio
{
    /// <summary>
    /// Interaction logic for ImageStudioImportWindow.xaml
    /// </summary>
    public partial class ImageStudioImportWindow : MusicDatabaseWindow
    {
        private object syncRoot = new object();

        private bool isAdding = false;
        private bool isGeneratingPreview = false;

        private ObservableCollection<InputImage> inputImages;
        private ObservableCollection<OutputImage> outputImages;

        public OutputImage[] OutputImages
        {
            get { return this.outputImages.ToArray(); }
        }

        public ImageStudioImportWindow()
        {
            InitializeComponent();

            this.inputImages = new ObservableCollection<InputImage>();
            this.outputImages = new ObservableCollection<OutputImage>();

            this.listInputImages.ItemsSource = this.inputImages;
            this.listOutputImages.ItemsSource = this.outputImages;

            this.UpdateButtons();

            try
            {
                string version = ImageMagick.VersionString;
                if (version.IndexOf("http://") != -1)
                {
                    version = version.Substring(0, version.IndexOf("http://") - 1);
                }
                this.labelImageMagickVersion.Text = version;
            }
            catch (Exception e)
            {
                this.labelImageMagickVersion.Text = "Unable to load ImageMagick";
                Utility.WriteToErrorLog(e.ToString());
            }
        }

        private void SaveImageCallback(Image image)
        {
            OutputImage outputImage = new OutputImage(image);
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                outputImage.InitSource();
                this.outputImages.Add(outputImage);
            }));
        }

        private int GetCurrentUpdateHash()
        {
            return Utility.GetCombinedHashCode(this.textPython.Text, Utility.GetCombinedHashCode(this.inputImages));
        }

        private void sourceImages_Drop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddInputImages(files);
        }

        private async void AddInputImages(string[] files)
        {
            this.isAdding = true;
            this.UpdateButtons();

            this.inputBusyIndicator.IsBusy = true;
            foreach (string file in files)
            {
                InputImage source = await Task.Run(() =>
                {
                    try
                    {
                        return new InputImage(file);
                    }
                    catch
                    {
                        return null;
                    }
                });

                if (source != null)
                {
                    source.InitSource();
                    this.inputImages.Add(source);
                }
            }
            this.inputBusyIndicator.IsBusy = false;

            this.isAdding = false;
            this.UpdateButtons();
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "Image Files|*.png;*.jpg;*.jpeg;*.tiff;*.gif;*.tif|All Files (*.*)|*.*";
            fileDialog.Multiselect = true;
            if (fileDialog.ShowDialog() == true)
            {
                this.AddInputImages(fileDialog.FileNames);
            }
        }

        private void UpdateButtons()
        {
            if (this.btnAdd == null || this.okCancelBox == null || this.btnPreview == null)
            {
                return;
            }

            bool enabled = !this.isAdding && !this.isGeneratingPreview;

            this.btnAdd.IsEnabled = enabled;
            this.btnPreview.IsEnabled = enabled;

            this.okCancelBox.IsOKEnabled = enabled
                && this.outputImages.Count > 0;
        }

        private void textPython_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.UpdateButtons();
        }

        private async Task<bool> ExecuteCode()
        {
            this.isGeneratingPreview = true;
            this.UpdateButtons();
            this.outputBusyIndicator.IsBusy = true;

            bool result = await this.ExecuteCodeInternal();

            this.isGeneratingPreview = false;
            this.UpdateButtons();
            this.outputBusyIndicator.IsBusy = false;

            return result;
        }

        private async Task<bool> ExecuteCodeInternal()
        {
            var inputImagesCopy = this.inputImages.ToArray();
            string rawCode = this.textPython.Text;

            this.outputImages.Clear();

            try
            {
                await Task.Run(() =>
                {
                    PythonExecutioner exec = new PythonExecutioner(this.SaveImageCallback);
                    exec.RunRawCode(inputImagesCopy, rawCode);
                });
            }
            catch (Exception ex)
            {
                Dialogs.Error(ex.Message);
                return false;
            }

            return true;
        }

        private void okCancelBox_OKClicked(object sender, EventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (Dialogs.Confirm())
            {
                object tag = ((FrameworkElement)sender).Tag;
                if (tag is InputImage)
                {
                    this.inputImages.Remove((InputImage)tag);
                }
                else if (tag is OutputImage)
                {
                    this.outputImages.Remove((OutputImage)tag);
                }
            }
        }

        private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            object tag = ((FrameworkElement)sender).Tag;
            if (tag is InputImage)
            {
                ViewImageWindow viewImageWindow = new ViewImageWindow(File.ReadAllBytes(((InputImage)tag).Path));
                viewImageWindow.ShowDialog();
            }
            else if (tag is OutputImage)
            {
                ViewImageWindow viewImageWindow = new ViewImageWindow(((OutputImage)tag).Bytes);
                viewImageWindow.ShowDialog();
            }
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            this.ExecuteCode();
        }
    }
}
