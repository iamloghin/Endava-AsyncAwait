using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileGenerator;

namespace FileConsumerAsync2
{
    internal class Program
    {
        private static async Task Main()
        {
            const int fileToProcess = 10;
            const int tasksLimit = 4;
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            var tokenSource = new CancellationTokenSource();

            var producer = new FilesGenerator(filePath);
            var consumer = new FileConsumerAsync2(filePath, fileToProcess, tasksLimit);
            var fileProducer = Task.Run(() => producer.GenerateFiles(tokenSource.Token), tokenSource.Token);

            var files = await consumer.StartAsync(tokenSource);
            await fileProducer;

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
