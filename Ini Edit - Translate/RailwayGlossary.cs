using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ini_Edit___Translate
{
    // Locale order: EN, DE, FR, PL. Only verified abbreviations are accepted as input.
    // Missing French ABW/IBW equivalents deliberately retain the original abbreviation.
    internal static class RailwayGlossary
    {
        private sealed class Entry
        {
            internal string[] Aliases, Values;
            internal Entry(string de, string en, string fr, string pl, string enAlias = "", string frAlias = "", string plAlias = "")
            { Aliases = new[] { enAlias, de, frAlias, plAlias }; Values = new[] { en, de, fr, pl }; }
        }
        private static readonly Entry[] Entries = {
            new Entry("DKW", "DSS", "TJD", "Rkpd", "DS|DSS", "TJD", "Rkpd"),
            new Entry("EKW", "SS", "TJS", "Rkp", "SS", "TJS", "Rkp"),
            new Entry("EW", "single turnout", "BS", "Rz", "", "BS", "Rz"),
            new Entry("ABW", "contrary-flexure turnout", "ABW", "Rłd", "", "", "Rłd"),
            new Entry("IBW", "similar-flexure turnout", "IBW", "Rłj", "", "", "Rłj"),
            new Entry("BW", "locomotive depot", "dépôt de locomotives", "lokomotywownia"),
            new Entry("Bf", "station", "gare", "stacja"),
            new Entry("Hbf", "main station", "gare principale", "dworzec główny"),
            new Entry("Hp", "halt", "halte ferroviaire", "przystanek osobowy", "", "", "PO"),
            new Entry("Gbf", "freight station", "gare de marchandises", "stacja towarowa"),
            new Entry("Rbf", "marshalling yard", "gare de triage", "stacja rozrządowa"),
            new Entry("Abzw", "junction", "bifurcation", "posterunek odgałęźny", "Jn", "", "PODG"),
            new Entry("BÜ", "level crossing", "passage à niveau", "przejazd kolejowo-drogowy", "LC", "PN"),
            new Entry("Stw", "signal box", "poste d’aiguillage", "nastawnia"),
            new Entry("EG", "station building", "bâtiment voyageurs", "budynek dworca", "", "BV"),
            new Entry("OL", "overhead line equipment", "caténaire", "sieć trakcyjna", "OLE"),
            new Entry("Gl", "track", "voie", "tor"),
            new Entry("Bstg", "platform", "quai", "peron"),
            new Entry("DWW", "three-way turnout", "branchement à trois voies", "rozjazd potrójny"),
            new Entry("Kr", "diamond crossing", "traversée de voies", "skrzyżowanie torów")
        };

        private static IEnumerable<string> Aliases(Entry entry, int language) =>
            entry.Aliases[language].Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

        internal static string Pattern(string from)
        {
            int source = Array.IndexOf(OpenAiTranslation.Languages, from);
            if (source < 0) throw new ArgumentException(Ui.Text("unsupportedLanguage"));
            var words = Entries.SelectMany(entry => Aliases(entry, source)).OrderByDescending(word => word.Length);
            return @"(?<![\p{L}\p{N}_])(?i:" + string.Join("|", words.Select(Regex.Escape)) + @")\.?" +
                @"(?![\p{L}\p{N}_])";
        }

        internal static bool TryTranslate(string text, string from, string to, out string translated)
        {
            int source = Array.IndexOf(OpenAiTranslation.Languages, from), target = Array.IndexOf(OpenAiTranslation.Languages, to);
            if (source < 0 || target < 0) throw new ArgumentException(Ui.Text("unsupportedLanguage"));
            string alias = text.TrimEnd('.');
            // Polish "po" is an ordinary preposition; only uppercase PO denotes a halt.
            if (from == "pl" && alias.Equals("po", StringComparison.OrdinalIgnoreCase) && alias != "PO")
            { translated = null; return false; }
            var entry = Entries.FirstOrDefault(item => Aliases(item, source).Any(word => string.Equals(word, alias, StringComparison.OrdinalIgnoreCase)));
            translated = entry == null ? null : entry.Values[target];
            if (entry != null && translated == entry.Values[source]) translated = text;
            else if (entry != null && text.EndsWith(".", StringComparison.Ordinal) &&
                !new[] { "Bf", "Hbf", "Hp", "Gbf", "Rbf", "Abzw", "Stw", "Gl", "Bstg", "Kr" }.Contains(entry.Values[1])) translated += ".";
            return entry != null;
        }

        internal sealed class Prepared
        {
            internal string Text;
            internal readonly Dictionary<string, string> Replacements = new Dictionary<string, string>();
            internal string Restore(string result)
            {
                OpenAiTranslation.ValidateTokens(Text, result);
                foreach (var pair in Replacements) result = result.Replace(pair.Key, pair.Value);
                return result;
            }
        }

        internal static Prepared Prepare(string text, string from, string to)
        {
            var result = new Prepared();
            string prefix = "IET_RAIL_";
            while (text.Contains(prefix)) prefix += "_";
            var parts = new Regex(OpenAiTranslation.Tokens + @"|[\p{L}\p{N}]+(?:_[\p{L}\p{N}]+)+|" + Pattern(from), RegexOptions.CultureInvariant);
            result.Text = parts.Replace(text, match => {
                if (!TryTranslate(match.Value, from, to, out string value) || value == match.Value) return match.Value;
                string marker = "{" + prefix + result.Replacements.Count + "}";
                result.Replacements.Add(marker, value);
                return marker;
            });
            return result;
        }
    }
}
