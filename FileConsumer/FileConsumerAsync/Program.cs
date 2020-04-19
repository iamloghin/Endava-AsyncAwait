using System;
using System.IO;
using System.Threading.Tasks;
using FileGenerator;

namespace FileConsumerAsync
{
    internal class Program
    {
        private static async Task Main()
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            const int fileToGenerate = 25;
            const int fileToProcess = 10;
            const int tasksLimit = 4;

            var producer = new FilesGenerator(filePath);
            var consumer = new FileConsumerAsync(filePath, fileToProcess, tasksLimit);
            var fileProducer = Task.Run(() => producer.GenerateFiles(fileToGenerate));

            var files = await consumer.StartAsync();
            await Task.WhenAll(fileProducer);
            
            Console.WriteLine($"Files processing complete of {files.Count}:", Console.ForegroundColor = ConsoleColor.Yellow);
            foreach (var file in files)
            {
                Console.WriteLine($"\t{file}");
            }
            Console.WriteLine("End.");
            Console.ReadLine();
        }
    }
}
