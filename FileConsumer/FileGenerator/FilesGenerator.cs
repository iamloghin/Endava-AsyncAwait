using System;
using System.IO;
using System.Threading.Tasks;

namespace FileGenerator
{
    public class FilesGenerator
    {
        private static readonly char[] Cons =
            {'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'z'};
        private static readonly char[] Vowel = { 'a', 'e', 'i', 'o', 'u', 'y' };
        private const int NrWordsOnEachFile = 1000;
        private static object _lock = new object();

        public static void Delete(string filePath)
        {
            var di = new DirectoryInfo(filePath);
            if (di.Exists)
            {
                foreach (var fileInfo in di.GetFiles())
                {
                    fileInfo.Delete();
                }
            }
        }

        public static void GenerateFiles(string filePath, int nrFiles)
        {
            var parentTask = new Task<string>(() =>
            {
                for (var i = 1; i <= nrFiles; i++)
                {
                    var fileNo = i;
                    new Task(() => GenerateWords(filePath, fileNo), TaskCreationOptions.AttachedToParent).Start();
                    Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(2, 7) * 100)).Wait();
                }

                return $"FileGenerator: Generated {nrFiles} files.";
            });

            parentTask.Start();
            Console.WriteLine(parentTask.Result, Console.ForegroundColor = ConsoleColor.Yellow);
        }

        private static void GenerateWords(string filePath, int fileNo)
        {
            var file = $"{filePath}/file.{fileNo}.dat";

            const long nrOfWords = NrWordsOnEachFile;

            var lines = new string[nrOfWords];

            var rand = new Random(100);

            for (var idx = 0; idx < nrOfWords; idx++) lines[idx] = GenerateWord(rand, rand.Next(1, 20));

            lock (_lock)
            {
                Console.WriteLine($"FileGenerator: {Path.GetFileName(file)}", Console.ForegroundColor = ConsoleColor.Blue);
                using (var outputFile = new StreamWriter(file))
                {
                    foreach (var line in lines) outputFile.WriteLine(line);
                }
            }
        }

        private static string GenerateWord(Random rand, int length)
        {
            if (length < 1) // do not allow words of zero length
                throw new ArgumentException("Length must be greater than 0");

            var word = string.Empty;

            if (rand.Next() % 2 == 0) // randomly choose a vowel or consonant to start the word
            {
                word += Cons[rand.Next(0, 20)];
            }
            else
            {
                word += Vowel[rand.Next(0, 4)];
            }

            for (var i = 1; i < length; i += 2) // the counter starts at 1 to account for the initial letter
            {
                // and increments by two since we append two characters per pass
                var c = Cons[rand.Next(0, 20)];
                var v = Vowel[rand.Next(0, 4)];

                word += c + v.ToString();
            }

            // the word may be short a letter because of the way the for loop above is constructed
            if (word.Length < length) // we'll just append a random consonant if that's the case
            {
                word += Cons[rand.Next(0, 20)];
            }

            return word;
        }
    }
}
