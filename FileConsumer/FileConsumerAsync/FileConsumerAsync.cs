using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileConsumerAsync
{
    internal class FileConsumerAsync
    {
        private const string processName = "FileConsumerAsync";
        private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private readonly List<string> filesConsumed = new List<string>();
        private readonly Queue<string> filesInQueue = new Queue<string>();
        private int Semaphore;
        private readonly CountdownEvent TasksCount;

        public FileConsumerAsync(string filePath, int fileToGenerate, int tasksLimit)
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

        public async Task<List<string>> StartAsync()
        {
            var token = TokenSource.Token;
            var tasks = new List<Task>();

            while (!TokenSource.Token.IsCancellationRequested)
            {
                if (!TasksCount.CurrentCount.Equals(0))
                {
                    if (filesInQueue.Count != 0 && Semaphore > 0)
                    {
                        string file;
                        lock (filesInQueue)
                        {
                            Semaphore--;
                            file = filesInQueue.Dequeue();
                        }
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await ProcessFile(file);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"{ex.Message}", Console.ForegroundColor = ConsoleColor.Magenta);
                            }
                        }, token));
                    }
                }
                else
                {
                    TokenSource.Cancel();
                    Console.WriteLine($"Cancellation token activated!", Console.ForegroundColor = ConsoleColor.Yellow);
                }
            }

            await Task.WhenAll(tasks.ToArray());
            return filesConsumed;
        }
        private async Task ProcessFile(string filesPath)
        {
            Task.Delay(TimeSpan.FromSeconds(new Random().Next(5))).Wait();

            var file = filesPath;

            if (TokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException($"{processName}: Cancel requested for {Path.GetFileName(file)}");
            }

            Console.WriteLine($"{processName}: {Path.GetFileName(file)}", Console.ForegroundColor = ConsoleColor.Red);
            lock (filesConsumed)
            {
                Semaphore++;
                filesConsumed.Add($"{Path.GetFileName(file)} - Thread id: {Thread.CurrentThread.ManagedThreadId}");
            }

            TasksCount.Signal();
        }

        private void AddNewFileTask(object sender, FileSystemEventArgs e)
        {
            filesInQueue.Enqueue(e.FullPath);
        }
    }
}
