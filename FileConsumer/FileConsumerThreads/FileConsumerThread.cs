using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FileConsumerThreads
{
    internal class FileConsumerThread
    {
        private volatile bool cancelToken;
        private readonly string processName;
        private readonly List<string> filesConsumed = new List<string>();
        private readonly Queue<string> filesInQueue = new Queue<string>();
        private readonly SemaphoreSlim semaphore;
        private readonly CountdownEvent threadsCount;

        public FileConsumerThread(string filePath, int fileToGenerate, int tasksLimit)
        {
            cancelToken = false;
            processName = nameof(FileConsumerThread);
            semaphore = new SemaphoreSlim(tasksLimit);
            threadsCount = new CountdownEvent(fileToGenerate);

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

        public List<string> GetFiles()
        {
            Monitor.Enter(filesConsumed);
                var files = filesConsumed;
            Monitor.Exit(filesConsumed);

            return files;
        }

        public Thread Start()
        {
            var mainWatcher = new Thread(Proceed);

            mainWatcher.Start();

            return mainWatcher;
        }

        private void Proceed()
        {
            while (!cancelToken)
            {
                if (!threadsCount.CurrentCount.Equals(0))
                {
                    if (filesInQueue.Count != 0)
                    {
                        semaphore.Wait();
                        ThreadPool.QueueUserWorkItem(ProcessFile, filesInQueue.Dequeue());
                    }
                }
                else
                {
                    cancelToken = true;
                    Console.WriteLine($"Cancellation token activated!", Console.ForegroundColor = ConsoleColor.Yellow);
                }
            }
        }

        private void ProcessFile(object filePath)
        {
            var file = filePath.ToString();
            Thread.Sleep(TimeSpan.FromMilliseconds(new Random().Next(500, 3000)));

            if (cancelToken)
            {
                Console.WriteLine($"{processName}: Cancel requested for {Path.GetFileName(file)}");
                return;
            }

            Console.WriteLine($"{processName}: {Path.GetFileName(file)}", Console.ForegroundColor = ConsoleColor.Red);

            Monitor.Enter(filesConsumed);
                filesConsumed.Add($"{Path.GetFileName(file)} - Thread no: {Thread.CurrentThread.ManagedThreadId}");
                semaphore.Release();
                threadsCount.Signal();
            Monitor.Exit(filesConsumed);
        }
    }
}
