using System;
using System.IO;
using System.Threading.Tasks;
using FileGenerator;

namespace FileConsumerTask
{
    internal class Program
    {
        private static void Main()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            const int fileToGenerate = 25;
            const int fileToProcess = 10;
            const int tasksLimit = 4;

            var producer = new FilesGenerator(filePath);
            var consumer = new FileConsumerTask(filePath, fileToProcess, tasksLimit).Start();
            var fileProducer = Task.Run(() => producer.GenerateFiles(fileToGenerate)).ConfigureAwait(false);

            var files = consumer.Result;
            fileProducer.GetAwaiter().GetResult();

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
