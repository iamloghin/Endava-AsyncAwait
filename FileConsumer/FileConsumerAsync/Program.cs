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

            FilesGenerator.Delete(filePath);

            var mainWatcher = new FileConsumerAsync(filePath, fileToProcess, tasksLimit);

            var fileGenerator = Task.Run(() => FilesGenerator.GenerateFiles(filePath, fileToGenerate));

            var files = await mainWatcher.StartAsync();

            await Task.WhenAll(fileGenerator);
            Console.WriteLine($"Files processing complete:", Console.ForegroundColor = ConsoleColor.Yellow);
            foreach (var file in files)
            {
                Console.WriteLine($"\t{file}");
            }
            Console.WriteLine("End.");
            Console.ReadLine();
        }
    }
}
