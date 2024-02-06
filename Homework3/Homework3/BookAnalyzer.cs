using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Homework3
{
    internal class BookAnalyzer
    {
        private readonly string _folderPath;
        private readonly string _outputFolderPath;
        private readonly ConcurrentDictionary<string, ProcessingStatus> _processingStatus;
        private readonly int _numberOfFiles;
        List<string> _proceededFiles;
        List<string> _startedProcessingFiles;

        public BookAnalyzer(string folderPath)
        {
            _numberOfFiles = Directory.GetFiles(folderPath, "*.txt").Length;
            _folderPath = folderPath;
            _outputFolderPath = Path.Combine(_folderPath, "Stats");
            _processingStatus = new ConcurrentDictionary<string, ProcessingStatus>();
            _proceededFiles = new List<string>();
            _startedProcessingFiles = new List<string>();
        }

        public async Task ProcessBooksAsync()
        {
            Console.WriteLine($"Files to proceed: {_numberOfFiles}");

            if (!Directory.Exists(_outputFolderPath))
            {
                Directory.CreateDirectory(_outputFolderPath);
            }

            string[] files = Directory.GetFiles(_folderPath, "*.txt");

            var tasks = files.Select(ProcessBookAsync);
            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookAsync(string path)
        {
            try
            {
                // Read the first line of the file to extract the book's title
                string firstLine = await ReadFirstLineAsync(path);
                string fileName = Path.GetFileName(path);

                // Extract the book's title from the first line and remove the specified prefix
                string title = ExtractBookTitle(firstLine);
                // INFO ABOUT STARTING PROCESSING A FILE
                //Console.WriteLine($"Processing file: {fileName}");
                _startedProcessingFiles.Add(title);

                // Construct the output file path using the extracted title
                string outputPath = Path.Combine(_outputFolderPath, $"{title}.txt");


                // Retry the file operation multiple times in case of failure
                int retryCount = 10;
                int delayMilliseconds = 1000;

                for (int i = 0; i < retryCount; i++)
                {
                    try
                    {
                        string bookContent = await File.ReadAllTextAsync(path);

                        var sentences = ExtractSentences(bookContent);
                        var words = ExtractWords(bookContent);
                        var punctuation = ExtractPunctuation(bookContent);

                        string longestSentence = sentences.OrderByDescending(s => s.Length).FirstOrDefault();
                        string shortestSentence = sentences.OrderBy(s => s.Split(' ').Length).FirstOrDefault();
                        string longestWord = words.OrderByDescending(w => w.Length).FirstOrDefault();
                        char mostCommonLetter = FindMostCommonLetter(bookContent);
                        var wordFrequency = CountWordFrequency(words);

                        using (StreamWriter writer = new StreamWriter(outputPath))
                        {
                            await writer.WriteLineAsync($"Longest Sentence: {longestSentence}");
                            await writer.WriteLineAsync($"Shortest Sentence: {shortestSentence}");
                            await writer.WriteLineAsync($"Longest Word: {longestWord}");
                            await writer.WriteLineAsync($"Most Common Letter: {mostCommonLetter}");
                            await writer.WriteLineAsync("Words Sorted by Frequency:");
                            foreach (var pair in wordFrequency)
                            {
                                await writer.WriteLineAsync($"{pair.Key}: {pair.Value}");
                            }
                        }

                        // INFO ABOUT END OF PROCESSING FILE
                        // Console.WriteLine($"Processing completed for file: {title}");
                        _proceededFiles.Add(title);
                        _startedProcessingFiles.Remove(title);
                        await showLoadingScreenAsync(_numberOfFiles, _startedProcessingFiles, _proceededFiles);
                        return; // Exit the loop if processing is successful
                    }
                    catch (IOException ex) when (i < retryCount - 1) // Retry on IOException
                    {
                        Console.WriteLine($"Error processing file: {ex.Message}. Retrying...");
                        await Task.Delay(delayMilliseconds); // Wait for a brief period before retrying
                    }
                }

                Console.WriteLine($"Error processing file: Failed after {retryCount} retries.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file: {ex.Message}");
            }
        }

        private async Task<string> ReadFirstLineAsync(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                return await reader.ReadLineAsync();
            }
        }

        private string ExtractBookTitle(string firstLine)
        {
            // Remove the specified prefix and trim any leading or trailing whitespace
            string prefixToRemove = "The Project Gutenberg eBook of";
            string title = firstLine.Replace(prefixToRemove, "").Trim();

            // Remove the colon if it exists
            title = title.Replace(":", "").Trim();

            return title;
        }

        private IEnumerable<string> ExtractSentences(string content)
        {
            List<string> sentences = new List<string>();
            StringBuilder currentSentence = new StringBuilder();

            // Define the skip patterns
            string[] skipPatterns = new string[]
            {
        "trademark owner",
        "Project Gutenberg™",
        "electronic works",
        "harmless from all liability",
        "CHAPTER",
        "Chapter",
        "CONTENTS",
        "Title:",
        "Project Gutenberg"
            };

            foreach (string line in content.Split('\n'))
            {
                // Check if the line contains any skip patterns
                if (skipPatterns.Any(pattern => line.Contains(pattern)))
                {
                    continue; // Skip this line
                }

                // Check if the line contains the start of a sentence
                if (line.Contains(".") || line.Contains("!") || line.Contains("?") || line.Contains(";"))
                {
                    // Append the line to the current sentence
                    currentSentence.Append(line.Trim());

                    // Add the current sentence to the list of sentences
                    sentences.Add(currentSentence.ToString().Trim());

                    // Clear the StringBuilder for the next sentence
                    currentSentence.Clear();
                }
                else
                {
                    // Append the line to the current sentence with a space separator
                    currentSentence.Append(line.Trim() + " ");
                }
            }

            return sentences;
        }



        private IEnumerable<string> ExtractWords(string content)
        {
            return Regex.Matches(content, @"(?:\b[\w']+(?:'\w+)?\b)")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .Where(word => !string.IsNullOrWhiteSpace(word));
        }



        private IEnumerable<char> ExtractPunctuation(string content)
        {
            return content.Where(char.IsPunctuation).Distinct();
        }

        private char FindMostCommonLetter(string content)
        {
            var letterFrequency = content.Where(char.IsLetter).GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
            return letterFrequency.OrderByDescending(pair => pair.Value).First().Key;
        }

        private Dictionary<string, int> CountWordFrequency(IEnumerable<string> words)
        {
            return words.GroupBy(w => w).ToDictionary(g => g.Key, g => g.Count()).OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private class ProcessingStatus
        {
            public bool IsProcessing { get; set; }
            public bool IsProcessed { get; set; }
            public int TotalBytes { get; set; }
            public int TotalWords { get; set; }
            public int TotalSentences { get; set; }
            public string LongestSentence { get; set; }
            public string ShortestSentence { get; set; }
            public string LongestWord { get; set; }
            public char MostCommonLetter { get; set; }
            public Dictionary<string, int> WordFrequency { get; set; }

            public ProcessingStatus()
            {
                IsProcessing = false;
                IsProcessed = false;
                TotalBytes = 0;
                TotalWords = 0;
                TotalSentences = 0;
                LongestSentence = string.Empty;
                ShortestSentence = string.Empty;
                LongestWord = string.Empty;
                MostCommonLetter = '\0';
                WordFrequency = new Dictionary<string, int>();
            }
        }

        private async Task showLoadingScreenAsync(int numberOfBooks, List<string> filesToProceed, List<string> proceededFiles)
        {
            Console.Clear();

            filesToProceed.Reverse();

            int progress = (int)(((double)proceededFiles.Count / numberOfBooks) * 100);
            if (numberOfBooks == proceededFiles.Count)
            {
                progress = 100;
            }
            int numberOfX = (int)Math.Round(progress / 10.0); // Adjust for a 10-unit scale for simplicity

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("Progress: " + new string('X', numberOfX) + new string(' ', 10 - numberOfX) + $" {progress}%");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("LAST 5 FILES PROCESSED:");
            Console.ResetColor();
            int startIndex = Math.Max(0, proceededFiles.Count - 5); // Show only the last 5 processed files
            for (int i = startIndex; i < proceededFiles.Count; i++)
            {
                Console.WriteLine(proceededFiles[i]);
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("LAST 8 FILES CURRENTLY PROCESSING:");
            Console.ResetColor();
            int startProcessingIndex = Math.Max(0, filesToProceed.Count - 8); // Show only the last 8 files being processed
            for (int i = startProcessingIndex; i < filesToProceed.Count; i++)
            {
                if (numberOfBooks == proceededFiles.Count)
                {
                    break;
                }

                Console.WriteLine(filesToProceed[i]);
            }
        }


    }
}
