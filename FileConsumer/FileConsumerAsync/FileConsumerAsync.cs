using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileConsumerAsync
{
    internal class FileConsumerAsync
    {
        private int Semaphore;
        private readonly string processName;
        private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private readonly List<string> filesConsumed = new List<string>();
        private readonly Queue<string> filesInQueue = new Queue<string>();
        private readonly CountdownEvent TasksCount;

        public FileConsumerAsync(string filePath, int fileToGenerate, int tasksLimit)
        {
            Semaphore = tasksLimit;
            processName = nameof(FileConsumerAsync);
            TasksCount = new CountdownEvent(fileToGenerate);

            var fileWatcher = new FileSystemWatcher(filePath)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            fileWatcher.Changed += (sender, e) =>
            {
                filesInQueue.Enqueue(e.FullPath);
            };

            Console.WriteLine($"{processName}: I will process {fileToGenerate} with max {tasksLimit} in parallel.");
        }

        public async Task<List<string>> StartAsync()
        {
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
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                ProcessFile(file);
                            }
                            catch (OperationCanceledException ex)
                            {
                                Console.WriteLine($"{ex.Message}", Console.ForegroundColor = ConsoleColor.Magenta);
                            }
                        }));
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
        private void ProcessFile(string filesPath)
        {
            Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(500, 3000))).Wait();

            if (TokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException($"{processName}: Cancel requested for {Path.GetFileName(filesPath)}");
            }

            Console.WriteLine($"{processName}: {Path.GetFileName(filesPath)}", Console.ForegroundColor = ConsoleColor.Red);
            lock (filesConsumed)
            {
                Semaphore++;
                filesConsumed.Add($"{Path.GetFileName(filesPath)} - Thread no: {Thread.CurrentThread.ManagedThreadId}");
                TasksCount.Signal();
            }
        }
    }
}
