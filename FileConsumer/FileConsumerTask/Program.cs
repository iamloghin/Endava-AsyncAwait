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

            FilesGenerator.Delete(filePath);

            var mainWatcher = new FileConsumerTask();
            var mainTask = mainWatcher.Start(filePath, fileToProcess, tasksLimit);
            var fileGenerator = Task.Run(() => FilesGenerator.GenerateFiles(filePath, fileToGenerate)).ConfigureAwait(false);

            var files = mainTask.Result;
            fileGenerator.GetAwaiter().GetResult();

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
