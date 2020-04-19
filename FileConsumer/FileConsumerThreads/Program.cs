using System;
using System.IO;
using System.Threading;
using FileGenerator;

namespace FileConsumerThreads
{
    internal class Program
    {
        private static void Main()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            const int fileToGenerate = 15;
            const int fileToProcess = 10;
            const int tasksLimit = 4;

            var producer = new FilesGenerator(filePath);
            var mainWatcher = new FileConsumerThread();
            var mainThread = mainWatcher.Start(filePath, fileToProcess, tasksLimit);
            new Thread(() => producer.GenerateFiles(fileToGenerate)).Start();

            mainThread.Join();
            var files = mainWatcher.GetFiles();
            Console.WriteLine("Files processing complete:");
            foreach (var file in files)
            {
                Console.WriteLine($"\t{file}");
            }
            Console.WriteLine("End.");
            Console.ReadLine();
        }
    }
}
