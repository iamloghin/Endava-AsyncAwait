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
            var consumer = new FileConsumerThread(filePath, fileToProcess, tasksLimit);
            var consumerThread = consumer.Start();
            new Thread(() => producer.GenerateFiles(fileToGenerate)).Start();

            consumerThread.Join();
            var files = consumer.GetFiles();

            Console.WriteLine($"Files processing complete of {files.Count}:");
            foreach (var file in files)
            {
                Console.WriteLine($"\t{file}");
            }
            Console.WriteLine("End.");
            Console.ReadLine();
        }
    }
}
