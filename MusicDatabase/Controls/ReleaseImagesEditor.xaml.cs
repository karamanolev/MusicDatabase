using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MusicDatabase.Engine;
using MusicDatabase.Engine.Entities;
using MusicDatabase.ImageStudio;
using Telerik.Windows.Controls;
using Image = MusicDatabase.Engine.Entities.Image;

namespace MusicDatabase
{
    /// <summary>
    /// Interaction logic for ReleaseImagesEditor.xaml
    /// </summary>
    public partial class ReleaseImagesEditor : UserControl, ICollectionImageHandler
    {
        public class ImageInfo : DependencyObject, INotifyPropertyChanged
        {
            public Image Image { get; set; }
            public byte[] Data { get; set; }

            public bool IsMain
            {
                get
                {
                    return this.Image.IsMain;
                }
                set
                {
                    this.Image.IsMain = value;
                    this.OnPropertyChanged("IsMain");
                    this.OnPropertyChanged("HeaderText");
                }
            }

            public string HeaderText
            {
                get
                {
                    string result = Utility.PascalCaseToString(this.Image.Type.ToString());
                    if (this.IsMain)
                    {
                        result += " (Main)";
                    }
                    return result;
                }
            }

            public ImageType Type
            {
                get { return this.Image.Type; }
                set
                {
                    this.Image.Type = value;
                    this.OnPropertyChanged("Type");
                    this.OnPropertyChanged("HeaderText");
                }
            }

            public Visibility EditorVisibility
            {
                get { return (Visibility)GetValue(EditorVisibilityProperty); }
                set { SetValue(EditorVisibilityProperty, value); }
            }
            public static readonly DependencyProperty EditorVisibilityProperty =
                DependencyProperty.Register("EditorVisibility", typeof(Visibility), typeof(ImageInfo), new UIPropertyMetadata(Visibility.Visible));

            public bool IsReadOnly
            {
                get { return (bool)GetValue(IsReadOnlyProperty); }
                set { SetValue(IsReadOnlyProperty, value); }
            }
            public static readonly DependencyProperty IsReadOnlyProperty =
                DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ImageInfo), new UIPropertyMetadata(false));

            public IEnumerable<object> ImageTypeComboItems
            {
                get { return ReleaseImagesEditor.GetImageTypeComboItems(); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName)
            {
                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
        }

        public static IEnumerable<object> GetImageTypeComboItems()
        {
            foreach (var item in Enum.GetValues(typeof(ImageType)))
            {
                yield return item;
            }
        }

        private Image[] originalReleaseImages;
        private Release release;

        public ObservableCollection<ImageInfo> Images { get; set; }
        public CollectionManager CollectionManager { get; set; }

        public Release Release
        {
            get
            {
                return this.release;
            }
            set
            {
                this.release = value;
                if (this.release == null)
                {
                    this.originalReleaseImages = null;
                }
                else
                {
                    this.originalReleaseImages = this.release.Images.ToArray();
                }
                this.OnReleaseChanged();
            }
        }

        public IEnumerable<object> ImageTypeComboItems
        {
            get
            {
                yield return "";
                foreach (var item in ReleaseImagesEditor.GetImageTypeComboItems())
                {
                    yield return item;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(ReleaseImagesEditor), new UIPropertyMetadata(false, OnIsReadOnlyPropertyChangedCallback));
        private static void OnIsReadOnlyPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ReleaseImagesEditor editor = (ReleaseImagesEditor)sender;
            editor.gridAddImage.Visibility = editor.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
            editor.tileView.IsItemDraggingEnabled = !editor.IsReadOnly;

            foreach (ImageInfo i in editor.Images)
            {
                i.EditorVisibility = editor.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
                i.IsReadOnly = editor.IsReadOnly;
            }
        }

        public bool IsMaximizationEnabled
        {
            get { return (bool)GetValue(IsMaximizationEnabledProperty); }
            set { SetValue(IsMaximizationEnabledProperty, value); }
        }
        public static readonly DependencyProperty IsMaximizationEnabledProperty =
            DependencyProperty.Register("IsMaximizationEnabled", typeof(bool), typeof(ReleaseImagesEditor), new PropertyMetadata(true, OnIsMaximizationEnabledPropertyChangedCallback));
        private static void OnIsMaximizationEnabledPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ReleaseImagesEditor editor = (ReleaseImagesEditor)sender;
            editor.tileView.MaximizeMode = editor.IsMaximizationEnabled ? TileViewMaximizeMode.ZeroOrOne : TileViewMaximizeMode.Zero;
        }

        public double ImageBoxWidth
        {
            get { return (double)GetValue(ImageBoxWidthProperty); }
            set { SetValue(ImageBoxWidthProperty, value); }
        }
        public static readonly DependencyProperty ImageBoxWidthProperty =
            DependencyProperty.Register("ImageBoxWidth", typeof(double), typeof(ReleaseImagesEditor), new UIPropertyMetadata(150d));

        public double ImageBoxHeight
        {
            get { return (double)GetValue(ImageBoxHeightProperty); }
            set { SetValue(ImageBoxHeightProperty, value); }
        }
        public static readonly DependencyProperty ImageBoxHeightProperty =
            DependencyProperty.Register("ImageBoxHeight", typeof(double), typeof(ReleaseImagesEditor), new UIPropertyMetadata(156d));

        public ReleaseImagesEditor()
        {
            this.Images = new ObservableCollection<ImageInfo>();
            this.DataContext = this;

            InitializeComponent();
        }

        private void OnReleaseChanged()
        {
            this.Images.Clear();

            if (this.Release != null)
            {
                foreach (Image image in this.Release.Images.ToArray())
                {
                    byte[] imageData;
                    try
                    {
                        imageData = this.CollectionManager.ImageHandler.LoadImage(image);
                    }
                    catch (Exception)
                    {
                        this.release.Images.Remove(image);
                        Dialogs.Error("Error reading image " + image.Id + " for release " + this.Release + ". It will be removed from the release if you save the changes.");
                        continue;
                    }
                    this.AddImage(image, imageData);
                }
            }
        }

        public bool AddImage(Image image, byte[] bytes)
        {
            bool anyImagesMain = this.Images.Any(i => i.Image.IsMain);
            if (image.IsMain && anyImagesMain)
            {
                this.Images.Where(i => i.Image.IsMain).ForEach(i => i.IsMain = false);
            }
            else if (!image.IsMain && !anyImagesMain)
            {
                image.IsMain = true;
            }

            this.Images.Add(new ImageInfo()
            {
                Image = image,
                Data = bytes,
                EditorVisibility = this.IsReadOnly ? Visibility.Collapsed : Visibility.Visible,
                IsReadOnly = this.IsReadOnly
            });

            //this.scrollViewer.ScrollToBottom();

            return true;
        }

        public void RemoveImage(Image image)
        {
            ImageInfo imageInfo = this.Images.Where(i => i.Image == image).First();
            this.Images.Remove(imageInfo);
            this.release.Images.Remove(image);
        }

        private void comboAddItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboAddItem.SelectedItem is ImageType)
            {
                ImageType type = (ImageType)this.comboAddItem.SelectedItem;

                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = true;
                dialog.Filter = Utility.DialogImageFilter;
                if (dialog.ShowDialog() == true)
                {
                    foreach (string file in dialog.FileNames)
                    {
                        this.AddFile(file, type);
                    }
                }

                this.comboAddItem.SelectedIndex = 0;
            }
        }

        private void AddFile(string fileName, ImageType type)
        {
            byte[] data;
            try
            {
                data = File.ReadAllBytes(fileName);
            }
            catch
            {
                Dialogs.Error("Unable to read file!");
                this.comboAddItem.SelectedIndex = 0;
                return;
            }

            Image image = new Image()
            {
                Type = type,
                Extension = Path.GetExtension(fileName),
                MimeType = MimeHelper.GetMimeTypeForExtension(Path.GetExtension(fileName))
            };
            if (this.AddImage(image, data))
            {
                this.Release.Images.Add(image);
            }
        }

        private void UserControl_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void UserControl_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }
            string[] droppedItems = (string[])e.Data.GetData(DataFormats.FileDrop);
            Array.Sort(droppedItems);
            foreach (string file in droppedItems)
            {
                if (!File.Exists(file))
                {
                    break;
                }

                ImageType type = ImageType.Other;
                this.AddFile(file, type);
            }
        }

        public void WriteFiles()
        {
            Assert.IsTrue(this.CollectionManager != null);
            foreach (ImageInfo imageInfo in this.Images)
            {
                this.CollectionManager.ImageHandler.StoreImage(imageInfo.Image, imageInfo.Data);
            }

            bool successfullyDeletedAllImages = true;
            foreach (Image deletedImage in this.originalReleaseImages.Except(this.release.Images))
            {
                try
                {
                    this.CollectionManager.ImageHandler.DeleteImage(deletedImage);
                }
                catch
                {
                    successfullyDeletedAllImages = false;
                }
            }
            if (!successfullyDeletedAllImages)
            {
                Dialogs.Inform("Unable to delete all image files. Please run cleanup.");
            }
        }

        private void tileView_TilesPositionChanged(object sender, Telerik.Windows.RadRoutedEventArgs e)
        {
            this.release.Images.Clear();
            this.release.Images.AddRange(this.Images.OrderBy(i => ((RadTileViewItem)this.tileView.ItemContainerGenerator.ContainerFromItem(i)).Position).Select(i => i.Image));
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (Dialogs.Confirm())
            {
                RadTileViewItem tile = ((FrameworkElement)sender).ParentOfType<RadTileViewItem>();
                ImageInfo imageInfo = (ImageInfo)this.tileView.ItemContainerGenerator.ItemFromContainer(tile);
                this.release.Images.Remove(imageInfo.Image);
                this.Images.Remove(imageInfo);

                if (this.release.Images.Count > 0 && !this.release.Images.Any(i => i.IsMain))
                {
                    Image image = this.release.Images[0];
                    ImageInfo mainImageInfo = this.Images.Where(i => i.Image == image).First();
                    mainImageInfo.IsMain = true;
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            RadTileViewItem tile = ((FrameworkElement)sender).ParentOfType<RadTileViewItem>();
            ImageInfo imageInfo = (ImageInfo)this.tileView.ItemContainerGenerator.ItemFromContainer(tile);
            Image image = imageInfo.Image;

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = image.Extension.Trim('.').ToUpper() + " Images (*" + image.Extension + ")|*" + image.Extension;
            if (dialog.ShowDialog() == true)
            {
                File.WriteAllBytes(dialog.FileName, imageInfo.Data);
            }
        }

        private void btnStudio_Click(object sender, RoutedEventArgs e)
        {
            ImageStudioImportWindow studioWindow = new ImageStudioImportWindow();
            if (studioWindow.ShowDialog(Window.GetWindow(this)) == true)
            {
                foreach (OutputImage outputImage in studioWindow.OutputImages)
                {
                    Image image = new Image()
                    {
                        Extension = outputImage.Extension,
                        MimeType = MimeHelper.GetMimeTypeForExtension(outputImage.Extension)
                    };

                    if (this.AddImage(image, outputImage.Bytes))
                    {
                        this.Release.Images.Add(image);
                    }
                }
            }
        }

        #region ICollectionImageHandler

        public byte[] LoadImage(Image image)
        {
            return this.Images.Where(i => i.Image == image).First().Data;
        }

        public void StoreImage(Image image, byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public void StoreImageFromXml(Image image, System.Xml.XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void DeleteImage(Image image)
        {
            throw new NotImplementedException();
        }

        public long GetImageByteLength(Image image)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
