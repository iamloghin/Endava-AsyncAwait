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
        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private static readonly List<string> filesConsumed = new List<string>();
        private static readonly Queue<string> filesInQueue = new Queue<string>();
        private static int Semaphore;
        private static CountdownEvent TasksCount;

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
            return await Proceed();
        }

        private void AddNewFileTask(object sender, FileSystemEventArgs e)
        {
            filesInQueue.Enqueue(e.FullPath);
        }

        private async Task<List<string>> Proceed()
        {
            var token = TokenSource.Token;
            var tasks = new List<Task>();

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
                    lock (filesInQueue)
                    {
                        Semaphore--;
                        file = filesInQueue.Dequeue();
                    }
                    tasks.Add(Task.Run(() => ProcessFile(file), token));
                }
            }

            try
            {
                await Task.WhenAll(tasks.ToArray());
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Your processFile tasks were canceled.", Console.ForegroundColor = ConsoleColor.Magenta);
            }
            return filesConsumed;
        }

        private static async Task ProcessFile(string filesPath)
        {
            Task.Delay(TimeSpan.FromSeconds(3)).Wait();

            var file = filesPath;

            if (TokenSource.Token.IsCancellationRequested)
            {
                Console.WriteLine($"{processName}: Cancel requested for {Path.GetFileName(file)}");
                return;
            }

            Console.WriteLine($"{processName}: {Path.GetFileName(file)}", Console.ForegroundColor = ConsoleColor.Red);
            lock (filesConsumed)
            {
                Semaphore++;
                filesConsumed.Add($"{Path.GetFileName(file)} - Thread id: {Thread.CurrentThread.ManagedThreadId}");
            }

            TasksCount.Signal();
        }
    }
}
