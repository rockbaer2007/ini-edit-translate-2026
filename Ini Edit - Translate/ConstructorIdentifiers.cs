using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ini_Edit___Translate
{
    internal static class ConstructorIdentifiers
    {
        internal static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "UGSo", "IET2026", "constructor-codes.txt");
        internal static readonly string[] DefaultCodes = LoadEmbedded();
        internal static readonly string[] Codes = Load();
        internal static readonly string Pattern = @"(?<![\p{L}\p{N}])(?i:" +
            string.Join("|", Codes.Select(Regex.Escape)) + @")(?![\p{L}\p{N}])";

        internal static void EnsureExternalFile(string destination = null)
        {
            string path = destination ?? FilePath;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                // CreateNew also protects a user file created concurrently by another instance.
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                    foreach (string code in DefaultCodes) writer.WriteLine(code);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { System.Diagnostics.Trace.WriteLine("Constructor list retained; using available external entries and built-in fallback."); }
        }

        internal static string[] Load(string source = null)
        {
            string path = source ?? FilePath;
            try
            {
                if (!File.Exists(path) || new FileInfo(path).Length > 65536) return DefaultCodes;
                var external = File.ReadAllLines(path, new UTF8Encoding(false, true))
                    .Select(line => line.Trim()).Where(line => Regex.IsMatch(line, @"^[A-Za-z]{2}[0-9]$"))
                    .Select(line => line.ToUpperInvariant());
                return DefaultCodes.Concat(external).Distinct(StringComparer.Ordinal).ToArray();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is DecoderFallbackException)
            { return DefaultCodes; }
        }

        private static string[] LoadEmbedded()
        {
            using (var stream = typeof(ConstructorIdentifiers).Assembly.GetManifestResourceStream("IET.ConstructorCodes.txt"))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException("Missing constructor code resource.")))
            {
                var codes = reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (codes.Length != 272 || codes.Any(code => !Regex.IsMatch(code, @"^[A-Z]{2}[0-9]$")) || codes.Distinct().Count() != codes.Length)
                    throw new InvalidOperationException("Invalid constructor code resource.");
                return codes;
            }
        }
    }
}
