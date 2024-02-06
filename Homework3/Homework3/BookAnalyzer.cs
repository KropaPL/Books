using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Homework3
{
    internal class BookAnalyzer
    {
        private readonly string _folderPath;

        public BookAnalyzer(string folderPath)
        {
            _folderPath = folderPath;
        }

        public async Task ProcessBooksAsync()
        {
            string[] files = Directory.GetFiles(_folderPath, "*.txt");

            var tasks = files.Select(ProcessBookAsync);
            await Task.WhenAll(tasks);
        }

        private async Task ProcessBookAsync(string path)
        {
            try
            {
                string bookContent = await File.ReadAllTextAsync(path);

                string fileName = Path.GetFileNameWithoutExtension(path);

                var sentences = ExtractSentences(bookContent);
                var words = ExtractWords(bookContent);
                var punctuation = ExtractPunctuation(bookContent);

                string longestSentence = sentences.OrderByDescending(s => s.Length).FirstOrDefault();
                string shortestSentence = sentences.OrderBy(s => s.Split(' ').Length).FirstOrDefault();
                string longestWord = words.OrderByDescending(w => w.Length).FirstOrDefault();
                char mostCommonLetter = FindMostCommonLetter(bookContent);
                var wordFrequency = CountWordFrequency(words);

                string outputPath = Path.Combine(_folderPath, $"{fileName}_stats.txt");
                using (StreamWriter writer = new StreamWriter(outputPath))
                {
                    await writer.WriteLineAsync($"Longest Sentence: {longestSentence}");
                    await writer.WriteLineAsync($"Shortest Sentence: {shortestSentence}");
                    await writer.WriteLineAsync($"Longest Word: {longestWord}");
                    await writer.WriteLineAsync($"Most Common Letter: {mostCommonLetter}");
                    await writer.WriteLineAsync("Word Frequency:");
                    foreach (var pair in wordFrequency)
                    {
                        await writer.WriteLineAsync($"{pair.Key}: {pair.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file: {ex.Message}");
            }
        }

        private IEnumerable<string> ExtractSentences(string content)
        {
            return Regex.Split(content, @"(?<=[\.!\?])\s+");
        }

        private IEnumerable<string> ExtractWords(string content)
        {
            return Regex.Matches(content, @"\b[\w']*\b").Select(m => m.Value);
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
    }
}
