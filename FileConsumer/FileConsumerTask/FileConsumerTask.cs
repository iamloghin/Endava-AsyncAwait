using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace FileConsumerTask
{
    internal class FileConsumerTask
    {
        private const string processName = "FileConsumerTask";
        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private static readonly List<string> filesConsumed = new List<string>();
        private static readonly Queue<string> filesInQueue = new Queue<string>();
        private static readonly object _lock = new object();
        private static int Semaphore;
        private static CountdownEvent TasksCount;

        public Task<List<string>> Start(string filePath, int fileToGenerate, int tasksLimit)
        {
            Initialize(filePath, fileToGenerate, tasksLimit);
            var mainWatcher = Proceed();

            mainWatcher.Start();
            return mainWatcher;
        }

        private void Initialize(string filePath, int fileToGenerate, int tasksLimit)
        {
            Console.WriteLine($"{processName}: I will process {fileToGenerate} with max {tasksLimit} in parallel.");

            Semaphore = tasksLimit;
            TasksCount = new CountdownEvent(fileToGenerate);

            var watcher = new FileSystemWatcher(filePath)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            watcher.Changed += AddNewFileTask;
        }

        private void AddNewFileTask(object sender, FileSystemEventArgs e)
        {
            filesInQueue.Enqueue(e.FullPath);
        }

        private Task<List<string>> Proceed()
        {
            return new Task<List<string>>(() =>
            {
                var token = TokenSource.Token;

                while (!TokenSource.Token.IsCancellationRequested)
                {
                    if (TasksCount.CurrentCount.Equals(0))
                    {
                        TokenSource.Cancel();
                        Console.WriteLine($"Cancellation token activated!", Console.ForegroundColor = ConsoleColor.Yellow);
                        continue;
                    }

                    if (filesInQueue.Count != 0 && Semaphore > 0)
                    {
                        string file;
                        lock (_lock)
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
                            catch (Exception ex)
                            {
                                Console.WriteLine($"{ex.Message}", Console.ForegroundColor = ConsoleColor.Magenta);
                            }
                        }, token, TaskCreationOptions.AttachedToParent, TaskScheduler.Default);
                    }
                }

                return filesConsumed;
            }, TokenSource.Token);
        }

        private void ProcessFile(string filesPath)
        {
            Task.Delay(TimeSpan.FromSeconds(new Random().Next(1,3))).Wait();

            var file = filesPath;

            if (TokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException($"{processName}: Cancel requested for {Path.GetFileName(file)}");
            }

            Console.WriteLine($"{processName}: {Path.GetFileName(file)}", Console.ForegroundColor = ConsoleColor.Red);
            lock (_lock)
            {
                Semaphore++;
                filesConsumed.Add($"{Path.GetFileName(file)} - Thread id: {Thread.CurrentThread.ManagedThreadId}");
            }

            TasksCount.Signal();
        }
    }
}
