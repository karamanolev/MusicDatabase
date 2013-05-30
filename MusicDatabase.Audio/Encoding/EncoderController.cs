using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using MusicDatabase.Engine;

namespace MusicDatabase.Audio.Encoding
{
    public class EncoderController : IEncoderController
    {
        private const int RetryAttempts = 1;

        private Task[] workerTasks;
        private Task workersCompletedTask;
        private BlockingCollection<IParallelTask> tasksRemaining;
        private bool shouldCancel = false;

        public EncoderControllerStatus Status { get; private set; }
        public IParallelTask[] Tasks { get; private set; }
        public bool DeleteSuccessfullyEncodedItems { get; set; }

        public EncoderController(IParallelTask[] tasks, int concurrency)
        {
            this.DeleteSuccessfullyEncodedItems = true;
            this.Tasks = tasks;

            this.tasksRemaining = new BlockingCollection<IParallelTask>();
            foreach (IParallelTask task in this.Tasks)
            {
                this.tasksRemaining.Add(task);
            }

            this.workerTasks = new Task[concurrency];
            for (int i = 0; i < this.workerTasks.Length; ++i)
            {
                this.workerTasks[i] = new Task(WorkerTask, i, TaskCreationOptions.LongRunning);
            }
            this.workersCompletedTask = Task.Factory.ContinueWhenAll(workerTasks, WorkersCompleted);

            this.Status = EncoderControllerStatus.NotStarted;
        }

        private void DeleteAllFiles()
        {
            foreach (IParallelTask task in this.Tasks)
            {
                task.EncoderFactory.TryDeleteResult(task);
            }
        }

        private void WorkersCompleted(Task[] workers)
        {
            if (this.Tasks.Any(t => t.Status == EncodeTaskStatus.Waiting || t.Status == EncodeTaskStatus.FaultedWaiting))
            {
                foreach (IParallelTask task in this.Tasks)
                {
                    if (task.Status == EncodeTaskStatus.FaultedWaiting)
                    {
                        task.Status = EncodeTaskStatus.Faulted;
                    }
                    else if (task.Status == EncodeTaskStatus.Waiting)
                    {
                        task.Status = EncodeTaskStatus.Cancelled;
                    }
                }

                if (this.DeleteSuccessfullyEncodedItems)
                {
                    this.DeleteAllFiles();
                }

                this.Status = EncoderControllerStatus.Faulted;
                this.OnCompleted();
                return;
            }

            if (this.Tasks.Any(t => t.Status == EncodeTaskStatus.Cancelled))
            {
                if (this.DeleteSuccessfullyEncodedItems)
                {
                    this.DeleteAllFiles();
                }

                foreach (IParallelTask task in this.Tasks)
                {
                    if (this.DeleteSuccessfullyEncodedItems || task.Status == EncodeTaskStatus.Waiting)
                    {
                        task.Status = EncodeTaskStatus.Cancelled;
                    }
                }

                this.Status = EncoderControllerStatus.Cancelled;
                this.OnCompleted();
                return;
            }

            if (this.Tasks.All(t => t.Status == EncodeTaskStatus.Completed || t.Status == EncodeTaskStatus.Skipped))
            {
                this.Status = EncoderControllerStatus.Completed;
                this.OnCompleted();
                return;
            }

            throw new InvalidOperationException("Unknown controller tasks result..." + string.Join(", ", this.Tasks.Select(t => t.Status.ToString())));
        }

        private void WorkerTask(object arg)
        {
            int threadNumber = (int)arg;
            int retriesRemaining = RetryAttempts;

            while (!this.shouldCancel)
            {
                IParallelTask task;

                if (!this.tasksRemaining.TryTake(out task, -1))
                {
                    break;
                }

                try
                {
                    task.Status = EncodeTaskStatus.Processing;
                    task.Progress = 0;

                    using (IEncoder encoder = task.EncoderFactory.CreateEncoder(threadNumber, task))
                    {
                        encoder.ProgressChanged += (sender, e) =>
                        {
                            task.Progress = e.Progress;
                            e.Cancel = this.shouldCancel;
                        };
                        encoder.Encode();
                        if (this.shouldCancel)
                        {
                            task.Status = EncodeTaskStatus.Cancelled;
                            this.tasksRemaining.CompleteAdding();
                            return;
                        }
                    }

                    task.Status = EncodeTaskStatus.Completed;
                    task.Progress = 1;
                }
                catch (SkipEncodingItemException e)
                {
                    task.Status = EncodeTaskStatus.Skipped;
                    task.Progress = 1;
                    task.EncoderFactory.TryDeleteResult(task);
                    Utility.WriteToErrorLog("Skipped encoding item: " + e.Message);
                }
                catch (Exception e)
                {
                    task.Status = EncodeTaskStatus.FaultedWaiting;

                    this.tasksRemaining.Add(task);

                    task.EncoderFactory.TryDeleteResult(task);
                    Utility.WriteToErrorLog(e.ToString());

                    if (retriesRemaining > 0)
                    {
                        --retriesRemaining;
                    }
                    else
                    {
                        break;
                    }
                }
                finally
                {
                    if (this.Tasks.All(t => t.Status == EncodeTaskStatus.Completed || t.Status == EncodeTaskStatus.Skipped))
                    {
                        this.tasksRemaining.CompleteAdding();
                    }
                }
            }
        }

        public void Start()
        {
            this.Status = EncoderControllerStatus.Running;
            foreach (Task task in this.workerTasks)
            {
                task.Start();
            }
        }

        public void Cancel()
        {
            this.shouldCancel = true;
            this.workersCompletedTask.Wait();
        }

        public event EventHandler Completed;
        private void OnCompleted()
        {
            if (this.Completed != null)
            {
                this.Completed(this, EventArgs.Empty);
            }
        }
    }
}
