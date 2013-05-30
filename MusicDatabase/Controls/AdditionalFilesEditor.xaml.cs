using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MusicDatabase.Engine.Entities;
using System.Windows.Data;

namespace MusicDatabase
{
    public partial class AdditionalFilesEditor : UserControl
    {
        private List<AdditionalFileEditor> items = new List<AdditionalFileEditor>();

        public Release Release
        {
            get { return (Release)GetValue(ReleaseProperty); }
            set { SetValue(ReleaseProperty, value); }
        }
        public static readonly DependencyProperty ReleaseProperty =
            DependencyProperty.Register("Release", typeof(Release), typeof(AdditionalFilesEditor), new UIPropertyMetadata(null, OnReleasePropertyChangedCallback));
        private static void OnReleasePropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((AdditionalFilesEditor)sender).OnReleaseChanged();
        }

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(AdditionalFilesEditor), new UIPropertyMetadata(false, OnIsReadOnlyPropertyChangedCallback));
        private static void OnIsReadOnlyPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            AdditionalFilesEditor editor = (AdditionalFilesEditor)sender;
            editor.gridAddFile.Visibility = editor.IsReadOnly ? Visibility.Collapsed : Visibility.Visible;
        }

        public AdditionalFilesEditor()
        {
            this.DataContext = this;

            InitializeComponent();

            this.comboAddItem.Items.Add("");
            foreach (var item in Enum.GetValues(typeof(ReleaseAdditionalFileType)))
            {
                this.comboAddItem.Items.Add(item);
            }
        }

        private void OnReleaseChanged()
        {
            foreach (var fileSelector in this.items)
            {
                this.grid.Children.Remove(fileSelector);
                this.grid.RowDefinitions.RemoveAt(this.grid.RowDefinitions.Count - 1);
            }
            this.items.Clear();

            if (this.Release != null)
            {
                foreach (ReleaseAdditionalFile file in this.Release.AdditionalFiles)
                {
                    this.AddItem(file);
                }
            }
        }

        private void AddItem(ReleaseAdditionalFile file)
        {
            this.grid.RowDefinitions.Add(new RowDefinition()
            {
                Height = new GridLength()
            });

            AdditionalFileEditor editor = CreateFileEditor();

            editor.FileType = file.Type;
            editor.SetFile(file.OriginalFilename, file.File);
            editor.FileDescription = file.Description;

            editor.DeleteClicked += new EventHandler(file_DeleteClicked);
            editor.AssociatedFile = file;
            editor.DataChanged += new EventHandler(editor_DataChanged);
            items.Add(editor);
            this.grid.Children.Add(editor);

            this.UpdateRows();
        }

        private AdditionalFileEditor CreateFileEditor()
        {
            AdditionalFileEditor editor = new AdditionalFileEditor();
            editor.Margin = new Thickness(0, 0, 2, 3);
            editor.SetBinding(AdditionalFileEditor.IsReadOnlyProperty, new Binding("IsReadOnly") { Source = this });
            return editor;
        }

        void editor_DataChanged(object sender, EventArgs e)
        {
            AdditionalFileEditor editor = (AdditionalFileEditor)sender;

            editor.AssociatedFile.Description = editor.FileDescription;
            editor.AssociatedFile.Type = editor.FileType;
        }

        private void UpdateRows()
        {
            for (int i = 0; i < items.Count; ++i)
            {
                Grid.SetRow(items[i], i);
            }
        }

        void file_DeleteClicked(object sender, System.EventArgs e)
        {
            AdditionalFileEditor editor = (AdditionalFileEditor)sender;
            this.Release.AdditionalFiles.Remove(editor.AssociatedFile);
            this.items.Remove(editor);
            this.grid.Children.Remove(editor);
            this.grid.RowDefinitions.RemoveAt(this.grid.RowDefinitions.Count - 1);
            this.UpdateRows();
        }

        private void comboAddItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboAddItem.SelectedItem is ReleaseAdditionalFileType)
            {
                ReleaseAdditionalFileType type = (ReleaseAdditionalFileType)this.comboAddItem.SelectedItem;

                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Multiselect = true;
                dialog.Filter = ReleaseAdditionalFile.GetFilterForType(type);
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

        private void AddFile(string fileName, ReleaseAdditionalFileType type)
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

            ReleaseAdditionalFile file = new ReleaseAdditionalFile()
            {
                Type = type,
                File = data,
                OriginalFilename = fileName
            };
            this.Release.AdditionalFiles.Add(file);
            this.AddItem(file);
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

                ReleaseAdditionalFileType type = ReleaseAdditionalFile.GetTypeForExtension(Path.GetExtension(file));
                this.AddFile(file, type);
            }
        }
    }
}
