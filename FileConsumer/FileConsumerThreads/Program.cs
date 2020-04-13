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

            FilesGenerator.Delete(filePath);

            var mainWatcher = FileConsumerThread.Start(filePath, fileToProcess, tasksLimit);
            new Thread(() => FilesGenerator.GenerateFiles(filePath, fileToGenerate)).Start();
            
            mainWatcher.Join();

            Console.WriteLine("Files processing complete:");
            foreach (var file in FileConsumerThread.GetFiles())
            {
                Console.WriteLine($"\t{file}");
            }
            Console.WriteLine("End.");
            Console.ReadLine();
        }
    }
}
