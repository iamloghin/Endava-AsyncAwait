using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileConsumerTask
{
    internal class FileConsumerTask
    {
        private int Semaphore;
        private readonly string processName;
        private readonly CountdownEvent tasksCount;
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();
        private readonly List<string> filesConsumed = new List<string>();
        private readonly Queue<string> filesInQueue = new Queue<string>();

        public FileConsumerTask(string filePath, int fileToGenerate, int tasksLimit)
        {
            Semaphore = tasksLimit;
            processName = nameof(FileConsumerTask);
            tasksCount = new CountdownEvent(fileToGenerate);

            var watcher = new FileSystemWatcher(filePath)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            watcher.Changed += (sender, e) =>
            {
                filesInQueue.Enqueue(e.FullPath);
            };

            Console.WriteLine($"{processName}: I will process {fileToGenerate} with max {tasksLimit} in parallel.");
        }

        public Task<List<string>> Start()
        {
            var consumerTask = Proceed();

            consumerTask.Start();

            return consumerTask;
        }

        private Task<List<string>> Proceed()
        {
            return new Task<List<string>>(() =>
            {
                while (!tokenSource.Token.IsCancellationRequested)
                {
                    if (!tasksCount.CurrentCount.Equals(0))
                    {
                        if (filesInQueue.Count != 0 && Semaphore > 0)
                        {
                            string file;
                            lock (filesInQueue)
                            {
                                Semaphore--;
                                file = filesInQueue.Dequeue();
                            }
                            Task.Factory.StartNew(() =>
                            {
                                try
                                {
                                    ProcessFile(file);
                                }
                                catch (OperationCanceledException ex)
                                {
                                    Console.WriteLine($"{ex.Message}", Console.ForegroundColor = ConsoleColor.Magenta);
                                }
                            }, tokenSource.Token, TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
                        }
                    }
                    else
                    {
                        tokenSource.Cancel();
                        Console.WriteLine($"Cancellation token activated!", Console.ForegroundColor = ConsoleColor.Yellow);
                    }
                }

                return filesConsumed;
            }, tokenSource.Token);
        }

        private void ProcessFile(string filesPath)
        {
            Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(500, 3000))).Wait();

            if (tokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException($"{processName}: Cancel requested for {Path.GetFileName(filesPath)}");
            }

            Console.WriteLine($"{processName}: {Path.GetFileName(filesPath)}", Console.ForegroundColor = ConsoleColor.Red);
            lock (filesConsumed)
            {
                Semaphore++;
                filesConsumed.Add($"{Path.GetFileName(filesPath)} - Thread no: {Thread.CurrentThread.ManagedThreadId}");
                tasksCount.Signal();
            }
        }
    }
}
