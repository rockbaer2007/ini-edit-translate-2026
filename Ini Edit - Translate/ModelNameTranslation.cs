using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ini_Edit___Translate
{
    // Only reviewed descriptive words are replaced. Unknown words are names/identifiers
    // until reviewed; they never reach an online translator through this path.
    internal static class ModelNameTranslation
    {
        private static readonly string[][] Words = {
            new[] { "house", "haus", "maison", "dom" },
            new[] { "houses", "häuser", "maisons", "domy" },
            new[] { "building", "gebäude", "bâtiment", "budynek" },
            new[] { "station", "bahnhof", "gare", "dworzec" },
            new[] { "platform", "bahnsteig", "quai", "peron" },
            new[] { "bridge", "brücke", "pont", "most" },
            new[] { "street", "straße", "rue", "ulica" },
            new[] { "roof", "dach", "toit", "dach" },
            new[] { "door", "tür", "porte", "drzwi" },
            new[] { "window", "fenster", "fenêtre", "okno" },
            new[] { "fence", "zaun", "clôture", "ogrodzenie" },
            new[] { "wall", "mauer", "mur", "mur" },
            new[] { "hall", "halle", "halle", "hala" },
            new[] { "shed", "schuppen", "remise", "szopa" },
            new[] { "factory", "fabrik", "usine", "fabryka" },
            new[] { "school", "schule", "école", "szkoła" },
            new[] { "church", "kirche", "église", "kościół" },
            new[] { "hotel", "hotel", "hôtel", "hotel" },
            new[] { "lamp", "lampe", "lampe", "lampa" },
            new[] { "lantern", "laterne", "lanterne", "latarnia" },
            new[] { "drive", "antrieb", "entraînement", "napęd" },
            new[] { "signal", "signal", "signal", "sygnał" },
            new[] { "turnout", "weiche", "aiguillage", "rozjazd" },
            new[] { "track", "gleis", "voie", "tor" },
            new[] { "locomotive", "lokomotive", "locomotive", "lokomotywa" },
            new[] { "wagon", "waggon", "wagon", "wagon" },
            new[] { "tree", "baum", "arbre", "drzewo" },
            new[] { "car", "auto", "voiture", "samochód" },
            new[] { "bus", "bus", "bus", "autobus" },
            new[] { "winter", "winter", "hiver", "zima" },
            new[] { "summer", "sommer", "été", "lato" },
            new[] { "left", "links", "gauche", "lewy" },
            new[] { "right", "rechts", "droite", "prawy" }
        };
        private static Regex Parts(string from, string dictionaryPattern) => new Regex(OpenAiTranslation.Tokens.ToString() +
            @"|[\p{L}\p{N}]+(?:_[\p{L}\p{N}]+)+|" + dictionaryPattern + "|" + RailwayGlossary.Pattern(from) +
            @"|(?<![\p{L}\p{N}_])\p{L}+(?![\p{L}\p{N}_])", RegexOptions.CultureInvariant);

        internal static bool HasDescriptiveWords(string text, string from)
        {
            int source = Array.IndexOf(OpenAiTranslation.Languages, from);
            if (source < 0) throw new ArgumentException(Ui.Text("unsupportedLanguage"));
            var words = new HashSet<string>(Words.Select(row => row[source]), StringComparer.OrdinalIgnoreCase);
            var external = DefaultDictionary.Load();
            return OpenAiTranslation.Languages.Where(to => to != from).Any(to =>
                Parts(from, external.Pattern(from, to)).Matches(text ?? "").Cast<Match>().Any(match =>
                    !OpenAiTranslation.Tokens.IsMatch(match.Value) && !match.Value.Contains("_") &&
                    (external.TryTranslate(match.Value, from, to, out _) || words.Contains(match.Value.Normalize(NormalizationForm.FormC)) ||
                    RailwayGlossary.TryTranslate(match.Value, from, to, out _))));
        }

        internal static string Translate(string text, string from, string to)
        {
            int source = Array.IndexOf(OpenAiTranslation.Languages, from), target = Array.IndexOf(OpenAiTranslation.Languages, to);
            if (source < 0 || target < 0) throw new ArgumentException(Ui.Text("unsupportedLanguage"));
            if (string.IsNullOrEmpty(text) || source == target) return text;
            var dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in Words) dictionary.Add(row[source], row[target]);
            var external = DefaultDictionary.Load();
            string result = Parts(from, external.Pattern(from, to)).Replace(text, match => {
                string word = match.Value;
                if (OpenAiTranslation.Tokens.IsMatch(word) || word.Contains("_")) return word;
                if (external.TryTranslate(word, from, to, out string preferred)) return preferred;
                if (RailwayGlossary.TryTranslate(word, from, to, out string term)) return term;
                if (!dictionary.TryGetValue(word.Normalize(NormalizationForm.FormC), out string translated)) return word;
                if (word.All(char.IsUpper)) return translated.ToUpperInvariant();
                if (char.IsUpper(word[0])) return char.ToUpperInvariant(translated[0]) + translated.Substring(1);
                return translated;
            });
            OpenAiTranslation.ValidateTokens(text, result);
            return result;
        }
    }
}
