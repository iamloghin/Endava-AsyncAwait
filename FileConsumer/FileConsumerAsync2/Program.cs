using System;
using System.IO;
using System.Threading.Tasks;
using FileGenerator;

namespace FileConsumerAsync2
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
            var consumer = new FileConsumerAsync2(filePath, fileToProcess, tasksLimit);
            var fileGenerator = Task.Run(() => producer.GenerateFiles(fileToGenerate));

            await consumer.StartAsync();
            await fileGenerator;

            var files = consumer.GetData();
            Console.WriteLine($"Files processing complete of {files.Count}:", Console.ForegroundColor = ConsoleColor.Yellow);
            Parallel.ForEach(files, file =>
            {
                Console.WriteLine($"\t{file.Key} - {file.Value}");
            });
            Console.WriteLine("End.");
            Console.ReadLine();
        }
    }
}
