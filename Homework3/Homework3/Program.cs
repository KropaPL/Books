using System;
using System.IO;
using System.Threading.Tasks;

namespace Homework3
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Provide path to the folder of books: ");
            string folderPath = Console.ReadLine();

            var analyzer = new BookAnalyzer(folderPath);
            await analyzer.ProcessBooksAsync();

            Console.WriteLine("Processing completed. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
