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
            const int fileToGenerate = 15;
            const int fileToProcess = 10;
            const int tasksLimit = 4;

            FilesGenerator.Delete(filePath);

            var mainWatcher = new FileConsumerAsync(filePath, fileToProcess, tasksLimit);

            _ = Task.Run(() => FilesGenerator.GenerateFiles(filePath, fileToGenerate));

            var files = await mainWatcher.StartAsync();

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
