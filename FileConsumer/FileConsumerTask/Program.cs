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
            const int fileToGenerate = 15;
            const int fileToProcess = 10;
            const int tasksLimit = 4;

            FilesGenerator.Delete(filePath);

            var mainWatcher = new FileConsumerTask();
            var mainTask = mainWatcher.Start(filePath, fileToProcess, tasksLimit);
            Task.Run(() => FilesGenerator.GenerateFiles(filePath, fileToGenerate)).ConfigureAwait(false);

            var files = mainTask.Result;

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
