using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileConsumerAsync2
{
    internal class FileConsumerAsync2
    {
        private readonly string processName;
        private static readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        private readonly ConcurrentDictionary<string, string> dataConsumed;
        private readonly ConcurrentQueue<string> dataInQueue;
        private readonly SemaphoreSlim semaphore;
        private readonly CountdownEvent countdownEvent;
        private CancellationTokenSource tokenSource;

        public FileConsumerAsync2(string filePath, int fileToConsume, int tasksLimit)
        {
            processName = nameof(FileConsumerAsync2);
            semaphore = new SemaphoreSlim(tasksLimit);
            countdownEvent = new CountdownEvent(fileToConsume);
            dataInQueue = new ConcurrentQueue<string>();
            dataConsumed = new ConcurrentDictionary<string, string>();

            var fileWatcher = new FileSystemWatcher(filePath)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.CreationTime |
                               NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };

            fileWatcher.Changed += (sender, e) =>
            {
                dataInQueue.Enqueue(e.FullPath);
            };

            Console.WriteLine($"{processName}: I will process {fileToConsume} with max {tasksLimit} in parallel.");
        }

        public ConcurrentDictionary<string, string> GetData()
        {
            return dataConsumed;
        }

        public async Task StartAsync()
        {
            var tasks = new List<Task>();

            while (!TokenSource.Token.IsCancellationRequested)
            {
                if (!countdownEvent.CurrentCount.Equals(0))
                {
                    var result = dataInQueue.TryDequeue(out var file);
                    if (result)
                    {
                        await semaphore.WaitAsync();
                        tasks.Add(Task.Run(() =>
                        {
                            try
                            {
                                ProcessFile(file);
                            }
                            catch (Exception ex)
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

            await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
        }
        private void ProcessFile(string file)
        {
            Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(500, 3000))).Wait();

            if (TokenSource.Token.IsCancellationRequested)
            {
                throw new OperationCanceledException($"{processName}: Cancel requested for {Path.GetFileName(file)}");
            }

            Console.WriteLine($"{processName}: {Path.GetFileName(file)}", Console.ForegroundColor = ConsoleColor.Red);
            dataConsumed.TryAdd(Path.GetFileName(file), $"Thread no {Thread.CurrentThread.ManagedThreadId.ToString()}");
            semaphore.Release();
            countdownEvent.Signal();
        }
    }
}
