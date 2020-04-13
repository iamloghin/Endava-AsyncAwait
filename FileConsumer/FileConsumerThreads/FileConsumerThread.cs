﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace FileConsumerThreads
{
    internal static class FileConsumerThread
    {
        private const string processName = "FileConsumerThread";
        private static volatile bool cancelToken;
        private static readonly List<string> filesConsumed = new List<string>();
        private static readonly Queue<string> filesInQueue = new Queue<string>();
        private static readonly object _lock = new object();
        private static SemaphoreSlim Semaphore;
        private static CountdownEvent ThreadsCount;

        public static List<string> GetFiles()
        {
            Monitor.Enter(_lock);
                var files = filesConsumed;
            Monitor.Exit(_lock);

            return files;
        }

        public static Thread Start(string filePath, int fileToGenerate, int tasksLimit)
        {
            Initialize(filePath, fileToGenerate, tasksLimit);
            var mainWatcher = new Thread(Proceed);

            mainWatcher.Start();
            return mainWatcher;
        }

        private static void Initialize(string filePath, int fileToGenerate, int tasksLimit)
        {
            Console.WriteLine($"{processName}: I will process {fileToGenerate} with max {tasksLimit} in parallel.");
            cancelToken = false;
            Semaphore = new SemaphoreSlim(tasksLimit);
            ThreadsCount = new CountdownEvent(fileToGenerate);

            var watcher = new FileSystemWatcher(filePath)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            watcher.Changed += AddNewFileTask;
        }

        private static void AddNewFileTask(object sender, FileSystemEventArgs e)
        {
            filesInQueue.Enqueue(e.FullPath);
        }

        private static void Proceed()
        {
            while (!cancelToken)
            {
                if (ThreadsCount.CurrentCount.Equals(0))
                {
                    cancelToken = true;
                    Console.WriteLine($"Cancellation token activated!", Console.ForegroundColor = ConsoleColor.Yellow);
                    continue;
                }

                if (filesInQueue.Count.Equals(0)) continue;

                if (Semaphore.CurrentCount > 0)
                {
                    Semaphore.Wait();
                    ThreadPool.QueueUserWorkItem(ProcessFile, filesInQueue.Dequeue());
                }
            }
        }

        private static void ProcessFile(object filePath)
        {
            var file = filePath.ToString();
            Thread.Sleep(TimeSpan.FromSeconds(3));

            if (cancelToken)
            {
                Console.WriteLine($"{processName}: Cancel requested for {Path.GetFileName(file)}");
                return;
            }

            Console.WriteLine($"{processName}: {Path.GetFileName(file)}", Console.ForegroundColor = ConsoleColor.Red);

            Monitor.Enter(_lock);
                filesConsumed.Add($"{Path.GetFileName(file)} - {Thread.CurrentThread.ManagedThreadId}");
                Semaphore.Release();
            Monitor.Exit(_lock);

            ThreadsCount.Signal();
        }
    }
}