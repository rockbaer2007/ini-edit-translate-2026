using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ini_Edit___Translate
{
    internal static class ConstructorIdentifiers
    {
        internal static readonly string[] Codes = Load();
        internal static readonly string Pattern = @"(?<![\p{L}\p{N}])(?i:" +
            string.Join("|", Codes.Select(Regex.Escape)) + @")(?![\p{L}\p{N}])";

        private static string[] Load()
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
