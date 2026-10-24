using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ini_Edit___Translate
{
    internal sealed class MyMemoryTranslation : IDisposable
    {
        internal sealed class Segment { internal string Text; internal bool Translate; }
        private readonly HttpClient client;
        private readonly string email;
        private readonly Dictionary<string, string> cache = new Dictionary<string, string>();
        private static readonly Regex Boundaries = new Regex(OpenAiTranslation.Tokens.ToString() +
            @"|\r\n|\r|\n|\t|\b[A-Z]{1,5}\d[\p{L}\p{N}_-]*\b|\b[A-Z]{2,8}\b|\b\d+(?:[.,:-]\d+)*\b", RegexOptions.CultureInvariant);

        internal MyMemoryTranslation(string email = null, HttpMessageHandler handler = null)
        {
            this.email = ValidateEmail(email);
            client = handler == null ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) : new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(45);
        }

        internal static string ValidateEmail(string email)
        {
            string value = (email ?? "").Trim();
            if (value.Length == 0) return "";
            try
            {
                var address = new System.Net.Mail.MailAddress(value);
                if (value.Length > 254 || address.Address != value || !address.Host.Contains(".") || Uri.CheckHostName(address.Host) != UriHostNameType.Dns) throw new FormatException();
                return value;
            }
            catch (FormatException) { throw new InvalidOperationException(Ui.Text("mmInvalidEmail")); }
        }

        internal static List<Segment> Split(string text)
        {
            var result = new List<Segment>(); int position = 0;
            foreach (Match match in Boundaries.Matches(text))
            {
                AddChunks(result, text.Substring(position, match.Index - position));
                result.Add(new Segment { Text = match.Value }); position = match.Index + match.Length;
            }
            AddChunks(result, text.Substring(position)); return result;
        }

        private static void AddChunks(List<Segment> result, string text)
        {
            var elements = StringInfo.ParseCombiningCharacters(text); int start = 0;
            while (start < elements.Length)
            {
                int end = start, bytes = 0, boundary = -1;
                while (end < elements.Length)
                {
                    int length = (end + 1 < elements.Length ? elements[end + 1] : text.Length) - elements[end];
                    int size = Encoding.UTF8.GetByteCount(text.Substring(elements[end], length));
                    if (bytes + size > 500) break;
                    bytes += size;
                    if (char.IsWhiteSpace(text, elements[end])) boundary = end + 1;
                    end++;
                }
                if (end == start) throw new InvalidOperationException(Ui.Text("mmSegmentTooLong"));
                if (end < elements.Length && boundary > start) end = boundary;
                string chunk = text.Substring(elements[start], (end < elements.Length ? elements[end] : text.Length) - elements[start]);
                int first = 0, last = chunk.Length;
                while (first < last && char.IsWhiteSpace(chunk[first])) first++;
                while (last > first && char.IsWhiteSpace(chunk[last - 1])) last--;
                if (first > 0) result.Add(new Segment { Text = chunk.Substring(0, first) });
                if (last > first) result.Add(new Segment { Text = chunk.Substring(first, last - first), Translate = true });
                if (last < chunk.Length) result.Add(new Segment { Text = chunk.Substring(last) });
                start = end;
            }
        }

        internal async Task<string> TranslateAsync(string text, string from, string to, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!OpenAiTranslation.Languages.Contains(from) || !OpenAiTranslation.Languages.Contains(to)) throw new ArgumentException(Ui.Text("unsupportedLanguage"));
            if (string.IsNullOrWhiteSpace(text) || from == to) return text;
            if (text.Length > 24000) throw new InvalidOperationException(Ui.Text("inputTooLong"));
            var segments = Split(text);
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(90)); var output = new StringBuilder();
                foreach (var segment in segments)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    if (!segment.Translate) { output.Append(segment.Text); continue; }
                    string cacheKey = from + "\0" + to + "\0" + segment.Text;
                    if (!cache.TryGetValue(cacheKey, out string value))
                    {
                        value = await GetAsync(segment.Text, from, to, deadline.Token).ConfigureAwait(false);
                        cache.Add(cacheKey, value);
                    }
                    output.Append(value);
                }
                string result = output.ToString(); OpenAiTranslation.ValidateTokens(text, result);
                cancellation.ThrowIfCancellationRequested(); return result;
            }
        }

        private async Task<string> GetAsync(string text, string from, string to, CancellationToken cancel)
        {
            string url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(text) +
                "&langpair=" + Uri.EscapeDataString(from + "|" + to) + "&mt=1" + (email.Length == 0 ? "" : "&de=" + Uri.EscapeDataString(email));
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel).ConfigureAwait(false))
                {
                    if ((int)response.StatusCode == 429) throw new InvalidOperationException(Ui.Text("mmLimit"));
                    if (!response.IsSuccessStatusCode) throw new InvalidOperationException(Ui.Format("mmHttp", (int)response.StatusCode));
                    using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (cancel.Register(() => stream.Dispose()))
                    using (var memory = new MemoryStream())
                    {
                        var buffer = new byte[4096]; int count;
                        while ((count = await stream.ReadAsync(buffer, 0, buffer.Length, cancel).ConfigureAwait(false)) > 0)
                        {
                            if (memory.Length + count > 128 * 1024) throw new InvalidOperationException(Ui.Text("mmResponse"));
                            memory.Write(buffer, 0, count);
                        }
                        JObject result;
                        try { result = JObject.Parse(new UTF8Encoding(false, true).GetString(memory.ToArray())); }
                        catch (Exception error) when (error is JsonException || error is DecoderFallbackException) { throw new InvalidOperationException(Ui.Text("mmResponse")); }
                        int status;
                        if (result["quotaFinished"]?.Type == JTokenType.Boolean && (bool)result["quotaFinished"]) throw new InvalidOperationException(Ui.Text("mmLimit"));
                        var statusToken = result["responseStatus"];
                        if ((statusToken?.Type != JTokenType.Integer && statusToken?.Type != JTokenType.String) ||
                            !int.TryParse(statusToken.ToString(), out status)) throw new InvalidOperationException(Ui.Text("mmResponse"));
                        if (status == 403 || status == 429) throw new InvalidOperationException(Ui.Text("mmLimit"));
                        if (status != 200) throw new InvalidOperationException(Ui.Format("mmHttp", status));
                        var data = result["responseData"] as JObject;
                        if (data?["translatedText"]?.Type != JTokenType.String) throw new InvalidOperationException(Ui.Text("mmResponse"));
                        string value = WebUtility.HtmlDecode((string)data["translatedText"]).Trim();
                        if (value.StartsWith("MYMEMORY WARNING:", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException(Ui.Text("mmLimit"));
                        if (value.Length == 0 || value.Contains("\r") || value.Contains("\n")) throw new InvalidOperationException(Ui.Text("mmResponse"));
                        return value;
                    }
                }
            }
            catch (Exception) when (cancel.IsCancellationRequested) { throw new OperationCanceledException(cancel); }
            catch (HttpRequestException) { throw new InvalidOperationException(Ui.Text("mmNetwork")); }
        }

        public void Dispose() { client.Dispose(); }
    }
}
