using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using MusicDatabase.Audio;
using CUETools.Codecs;
using System.Collections.Concurrent;
using MusicDatabase.Engine;

namespace MusicDatabase.Advanced
{
    /// <summary>
    /// Interaction logic for AudioChecksumWindow.xaml
    /// </summary>
    public partial class AudioChecksumWindow : MusicDatabaseWindow
    {
        public class AudioChecksumItem
        {
            public string Path { get; set; }
            public string Filename { get; set; }
            public string Checksum { get; set; }
        }

        private int totalFiles = 0, processedFiles = 0;
        private BlockingCollection<string> tasks;
        private Task workerTask;
        private bool shouldCancel;

        public override bool SaveWindowSettings
        {
            get { return false; }
        }

        public AudioChecksumWindow()
        {
            this.tasks = new BlockingCollection<string>();
            this.workerTask = new Task(this.WorkerTask);
            this.workerTask.Start();

            InitializeComponent();
        }

        private void WorkerTask()
        {
            while (true)
            {
                string item;
                if (this.shouldCancel || !this.tasks.TryTake(out item, -1))
                {
                    break;
                }

                try
                {
                    if (AudioHelper.IsSupportedAudioSource(item))
                    {
                        this.ProcessAudio(AudioHelper.GetAudioSourceForFile(item), Path.GetDirectoryName(item), Path.GetFileName(item));
                    }
                    else if (item.ToLower().EndsWith(".cue"))
                    {
                        CueSheet sheet = new CueSheet(item);

                        bool isOriginal;
                        string target = sheet.DiscoverTarget(out isOriginal);
                        if (target == null)
                        {
                            Dialogs.Error("Cue target not found.");
                            continue;
                        }
                        for (int i = 0; i < sheet.Tracks.Length; ++i)
                        {
                            IAudioSource fullFileSource = AudioHelper.GetAudioSourceForFile(target);
                            AudioSourcePart sourcePart = new AudioSourcePart(fullFileSource);
                            sourcePart.SetLengthFrames(
                                sheet.GetTrackStartFrame(i),
                                sheet.GetTrackEndFrame(i));
                            this.ProcessAudio(sourcePart, target, (i + 1).ToString("00") + ". " + sheet.Tracks[i].Title);
                        }
                    }
                }
                catch (Exception e)
                {
                    Utility.WriteToErrorLog(e.ToString());
                    Dialogs.Error("Error processing file: " + e.Message);
                }
            }
        }

        private void ProcessAudio(IAudioSource file, string displayPath, string displayFilename)
        {
            this.Dispatcher.BeginInvokeAction(() =>
            {
                this.textStatus.Text = this.processedFiles + " / " + this.totalFiles;
                this.progressChecksum.Value = 0;
            });

            AudioChecksumCalculator calculator = new AudioChecksumCalculator(file);
            calculator.ProgressChanged +=
                ProgressBarUpdater.CreateHandler(this.Dispatcher, this.progressChecksum, () => this.shouldCancel);
            uint crc32 = calculator.GetCRC32();
            ++this.processedFiles;

            file.Close();

            this.Dispatcher.BeginInvokeAction(() =>
            {
                this.textStatus.Text = this.processedFiles + " / " + this.totalFiles;
                this.listChecksums.Items.Add(new AudioChecksumItem()
                {
                    Path = displayPath,
                    Filename = displayFilename,
                    Checksum = crc32.ToString("X8")
                });
            });
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Array.Sort(files);
            foreach (string file in files)
            {
                this.AddItem(file);
            }
        }

        public void AddItem(string file)
        {
            if (AudioHelper.IsSupportedAudioSource(file))
            {
                ++this.totalFiles;
                this.tasks.Add(file);
            }
            else if (file.ToLower().EndsWith(".cue"))
            {
                try
                {
                    this.totalFiles += new CueSheet(file).Tracks.Length;
                    this.tasks.Add(file);
                }
                catch
                {
                }
            }

            this.textStatus.Text = this.processedFiles + " / " + this.totalFiles;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.shouldCancel = true;
            this.tasks.CompleteAdding();
            this.workerTask.Wait();
        }
    }
}
