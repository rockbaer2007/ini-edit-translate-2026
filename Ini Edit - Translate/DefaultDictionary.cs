using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic.FileIO;

namespace Ini_Edit___Translate
{
    internal sealed class DefaultDictionary
    {
        internal static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UGSo", "IET2026", "translation-dictionary.csv");
        private readonly List<string[]> rows;
        private DefaultDictionary(List<string[]> rows) { this.rows = rows; }

        private static Stream Embedded() => typeof(DefaultDictionary).Assembly.GetManifestResourceStream("IET.DefaultDictionary.csv")
            ?? throw new InvalidOperationException("Missing default dictionary resource.");

        internal static void EnsureExternalFile(string destination = null)
        {
            string path = destination ?? FilePath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var input = Embedded())
                using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read)) input.CopyTo(output);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { System.Diagnostics.Trace.WriteLine("Existing translation dictionary retained; built-in fallback available."); }
        }

        internal static DefaultDictionary Load(string path = null)
        {
            List<string[]> defaults;
            using (var stream = Embedded()) defaults = Read(stream);
            path = path ?? FilePath;
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > 1024 * 1024) return new DefaultDictionary(defaults);
                using (var stream = File.OpenRead(path))
                {
                    // External rows come first, including when overriding a shipped entry.
                    var external = Read(stream);
                    external.AddRange(defaults);
                    return new DefaultDictionary(external);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                error is DecoderFallbackException || error is MalformedLineException || error is InvalidDataException)
            {
                System.Diagnostics.Trace.WriteLine("Translation dictionary unreadable; using built-in fallback without changing the file.");
                return new DefaultDictionary(defaults);
            }
        }

        private static List<string[]> Read(Stream stream)
        {
            using (var parser = new TextFieldParser(stream, new UTF8Encoding(false, true), true))
            {
                parser.SetDelimiters(";"); parser.HasFieldsEnclosedInQuotes = true;
                string[] header = parser.ReadFields();
                if (header == null || !header.SequenceEqual(new[] { "Deutsch", "ENG", "FRA", "POL" }))
                    throw new InvalidDataException("Dictionary header must be Deutsch;ENG;FRA;POL.");
                var entries = new List<string[]>();
                while (!parser.EndOfData)
                {
                    string[] row = parser.ReadFields();
                    if (row == null || row.Length != 4 || row.Any(value => value.Length > 200 || value.Any(char.IsControl)) || entries.Count >= 2000)
                        throw new InvalidDataException("Invalid dictionary row.");
                    // Empty target cells fall back to the next matching entry/provider.
                    entries.Add(new[] { row[1], row[0], row[2], row[3] });
                }
                return entries;
            }
        }

        internal string Pattern(string from, string to)
        {
            int source = Language(from), target = Language(to);
            var words = rows.Where(row => row[source].Length > 0 && row[target].Length > 0)
                .Select(row => row[source]).Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(word => word.Length);
            string alternatives = string.Join("|", words.Select(Regex.Escape));
            return alternatives.Length == 0 ? "(?!)" : @"(?<![\p{L}\p{N}_])(?i:" + alternatives + @")(?![\p{L}\p{N}_])";
        }

        private static int Language(string code)
        {
            int index = Array.IndexOf(OpenAiTranslation.Languages, code);
            if (index < 0) throw new ArgumentException(Ui.Text("unsupportedLanguage"));
            return index;
        }

        internal bool TryTranslate(string word, string from, string to, out string result)
        {
            int source = Language(from), target = Language(to);
            var row = rows.FirstOrDefault(item => item[target].Length > 0 && string.Equals(item[source], word, StringComparison.OrdinalIgnoreCase));
            result = row == null ? null : row[target];
            return row != null;
        }

        internal RailwayGlossary.Prepared Prepare(string text, string from, string to)
        {
            var prepared = new RailwayGlossary.Prepared();
            string prefix = "IET_DICT_";
            while (text.Contains(prefix)) prefix += "_";
            var parts = new Regex(OpenAiTranslation.Tokens + @"|[\p{L}\p{N}]+(?:_[\p{L}\p{N}]+)+|" + Pattern(from, to), RegexOptions.CultureInvariant);
            prepared.Text = parts.Replace(text, match => {
                // A protected identifier can never be overridden by a dictionary row.
                if (OpenAiTranslation.Tokens.IsMatch(match.Value) || match.Value.Contains("_") ||
                    !TryTranslate(match.Value, from, to, out string translated)) return match.Value;
                string marker = "{" + prefix + prepared.Replacements.Count + "}";
                prepared.Replacements.Add(marker, translated);
                return marker;
            });
            return prepared;
        }
    }
}
