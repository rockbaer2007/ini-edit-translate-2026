using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Ini_Edit___Translate;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class MyMemoryTests
{
    private static void Assert(bool value, string reason) { if (!value) throw new Exception(reason); }
    private sealed class Handler : HttpMessageHandler
    {
        internal int Calls;
        internal readonly List<string> Queries = new List<string>();
        internal Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Reply;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert(request.Method == HttpMethod.Get && request.RequestUri.GetLeftPart(UriPartial.Path) == "https://api.mymemory.translated.net/get", "GET endpoint only");
            Assert(request.Headers.Authorization == null && request.Content == null, "no credentials or upload");
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            Assert(query["key"] == null && query["user"] == null && query["pass"] == null, "no authentication parameters");
            Assert(Encoding.UTF8.GetByteCount(query["q"]) <= 500 && !query["q"].Contains("\n") && !query["q"].Contains("\r"), "byte and paragraph limits");
            Queries.Add(query["q"]);
            return Reply == null ? Task.FromResult(Response(query["q"])) : Reply(request, token);
        }
    }
    private sealed class ForbiddenTokens : IChatGptTokens
    { public Task<string> GetAccessTokenAsync(string clientId, CancellationToken token) { throw new Exception("OAuth must not be used"); } }
    private static HttpResponseMessage Response(string text, int status = 200, bool quota = false) => new HttpResponseMessage(HttpStatusCode.OK)
    { Content = new StringContent(new JObject { ["responseStatus"] = status, ["quotaFinished"] = quota, ["responseData"] = new JObject { ["translatedText"] = text } }.ToString(), Encoding.UTF8, "application/json") };
    private static string Translate(OpenAiTranslation service, string text, string target = "de") => service.TranslateAsync(text, "en", target, CancellationToken.None).GetAwaiter().GetResult();
    private static void Fails(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new Exception("Expected controlled failure"); }

    internal static void Run(Action<string, Action> test)
    {
        test("EEP line-break controls, complete constructor tags and game versions stay outside MyMemory requests", () =>
        {
            string source = "Antrieb (GK3) für EEP16.2[e]Laterne (AG3)[e]eep16.2 (DL1) EEP 16.2 EEP17";
            var handler = new Handler { Reply = (request, token) => {
                string text = HttpUtility.ParseQueryString(request.RequestUri.Query)["q"];
                Assert(!text.Contains("GK3") && !text.Contains("AG3") && !text.Contains("DL1") && !text.Contains("EEP") && !text.Contains("eep") && !text.Contains("[e]") && !text.Contains("(") && !text.Contains(")"), "protected tokens never submitted, including their delimiters");
                return Task.FromResult(Response(text == "Antrieb" ? "Drive" : text == "für" ? "for" : text == "Laterne" ? "Lamp" : text));
            } };
            using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, handler))
            {
                string result = service.TranslateAsync(source, "de", "en", CancellationToken.None).GetAwaiter().GetResult();
                Assert(result == "Drive (GK3) for EEP16.2[e]Lamp (AG3)[e]eep16.2 (DL1) EEP 16.2 EEP17", "exact literals restored in their original sequence and boundaries");
            }
        });
        test("constructor recognition is generic and absent identifiers are never invented", () =>
        {
            foreach (string code in new[] { "(GK3)", "(AG3)", "(DL1)", "(ab7)", "(XY12)", "(7AB)", "(A9B)", "(AB-CD3)", "( AB12 )" })
            {
                var handler = new Handler();
                using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, handler))
                    Assert(Translate(service, "Lamp " + code) == "Lamp " + code, "generic identifier retained: " + code);
                Assert(handler.Queries.SequenceEqual(new[] { "Lamp" }), "no constructor list required");
            }
            foreach (string addition in new[] { "", " (Winter)", " (Sommer)" })
            {
                var parts = MyMemoryTranslation.Split("Lamp" + addition);
                Assert(!parts.Any(part => !part.Translate && part.Text.Contains("Winter")), "ordinary words remain translatable");
                var handler = new Handler();
                using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, handler))
                    Assert(Translate(service, "Lamp" + addition) == "Lamp" + addition, "no identifier appended");
            }
            Fails(() => OpenAiTranslation.ValidateTokens("Lamp", "Lampe (GK3)"));
        });
        test("legacy bare constructor codes remain intact at the start middle or end", () =>
        {
            foreach (string source in new[] { "GK3 Lamp", "ab7 Lamp", "Lamp GK3", "Lamp GK3 Drive", "GK3_Lamp", "GK3-Lamp", "Lamp (ab7)", "XY12: Lamp" })
            {
                var handler = new Handler();
                using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, handler))
                    Assert(Translate(service, source) == source, "legacy position and separators retained");
                Assert(handler.Queries.All(query => !query.Contains("GK3") && !query.Contains("ab7") && !query.Contains("XY12")), "bare identifiers excluded from online query");
            }
            Fails(() => OpenAiTranslation.ValidateTokens("GK3 Drive", "KG3 Antrieb"));
            Fails(() => OpenAiTranslation.ValidateTokens("Drive", "Antrieb GK3"));
        });
        test("German and French use English as pivot and cache the intermediate translation", () =>
        {
            foreach (string source in new[] { "de", "fr" })
            {
                var pairs = new System.Collections.Generic.List<string>();
                var handler = new Handler { Reply = (request, token) => {
                    var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
                    pairs.Add(query["langpair"]);
                    Assert(query["q"] == (pairs.Count == 1 ? "Original" : "English"), "intermediate input");
                    return Task.FromResult(Response(pairs.Count == 1 ? "English" : "Target"));
                } };
                using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, handler))
                {
                    Assert(service.TranslateAsync("Original", source, source == "de" ? "fr" : "de", CancellationToken.None).GetAwaiter().GetResult() == "Target", "pivot output");
                    Assert(service.TranslateAsync("Original", source, "en", CancellationToken.None).GetAwaiter().GetResult() == "English", "cached intermediate");
                    Assert(pairs.SequenceEqual(new[] { source + "|en", source == "de" ? "en|fr" : "en|de" }), "no direct DE/FR request");
                }
            }
        });
        test("MyMemory selection never uses OpenAI credentials and caches per language", () =>
        {
            var handler = new Handler { Reply = (request, token) =>
            {
                var q = HttpUtility.ParseQueryString(request.RequestUri.Query);
                Assert(q["de"] == null && q["q"] == "Sun", "only unprotected text sent anonymously");
                return Task.FromResult(Response(q["langpair"] == "en|de" ? "Sonne" : "Soleil"));
            } };
            using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true, UseChatGpt = true, ProtectedKey = "invalid-key-must-not-be-read" }, handler, new ForbiddenTokens()))
            {
                string source = "  V140 Sun {0}\r\nSun\t1935 %s [e] <b>\\n";
                Assert(Translate(service, source) == "  V140 Sonne {0}\r\nSonne\t1935 %s [e] <b>\\n", "codes, placeholders and whitespace retained");
                Assert(Translate(service, source).Contains("Sonne") && handler.Calls == 1, "same-operation cache");
                Assert(Translate(service, source, "fr").Contains("Soleil") && handler.Calls == 2, "target-language cache separation");
            }
        });
        test("MyMemory UTF8 segmentation preserves source and text elements", () =>
        {
            string source = string.Concat(Enumerable.Repeat("énergie 🚂 a\u0308 ", 200)) + "\r\n" + new string('a', 1001) + " {0} V140";
            var parts = MyMemoryTranslation.Split(source);
            Assert(string.Concat(parts.Select(p => p.Text)) == source, "lossless segmentation");
            Assert(parts.Where(p => p.Translate).All(p => Encoding.UTF8.GetByteCount(p.Text) <= 500 && !p.Text.Contains("\r") && !p.Text.Contains("\n")), "each query obeys limits");
            var handler = new Handler();
            using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, handler)) Assert(Translate(service, source) == source, "lossless end-to-end reconstruction");
            Assert(handler.Calls > 1, "large input split");
        });
        test("MyMemory email is voluntary and HTML entities are decoded once", () =>
        {
            Assert(MyMemoryTranslation.ValidateEmail(" ") == "", "email optional");
            Fails(() => MyMemoryTranslation.ValidateEmail("person@example.org&key=bad"));
            Fails(() => MyMemoryTranslation.ValidateEmail("Person <person@example.org>"));
            var handler = new Handler { Reply = (request, token) =>
            {
                Assert(HttpUtility.ParseQueryString(request.RequestUri.Query)["de"] == "test@example.org", "optional contact email");
                return Task.FromResult(Response("A &amp; B"));
            } };
            using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true, MyMemoryEmail = "test@example.org" }, handler)) Assert(Translate(service, "A and B") == "A & B", "decode provider output");
        });
        test("MyMemory settings preserve encrypted key and ChatGPT connection", () =>
        {
            string directory = Path.Combine(Path.GetTempPath(), "iet-mm-" + Guid.NewGuid().ToString("N"));
            try
            {
                string key = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes("PRIVATE-FIXTURE"), null, DataProtectionScope.CurrentUser));
                var settings = new OpenAiSettings { UseMyMemory = true, UseChatGpt = true, ChatGptClientId = "client-fixture", ChatGptModel = "model-fixture", ProtectedKey = key };
                string file = Path.Combine(directory, "settings.json"); settings.Save(null, file);
                var loaded = JsonConvert.DeserializeObject<OpenAiSettings>(File.ReadAllText(file));
                Assert(loaded.UseMyMemory && loaded.UseChatGpt && loaded.ProtectedKey == key && loaded.ChatGptClientId == settings.ChatGptClientId && loaded.ChatGptModel == settings.ChatGptModel, "existing credentials and selection metadata retained");
                Assert(!File.ReadAllText(file).Contains("PRIVATE-FIXTURE"), "no plaintext key");
                Assert(!JsonConvert.DeserializeObject<OpenAiSettings>("{\"UseChatGpt\":true}").UseMyMemory, "legacy selection unchanged");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        });
        test("MyMemory quotas and malformed results never become translations or trigger fallback", () =>
        {
            foreach (string body in new[] { "not JSON SECRET-FIXTURE", "{}", "{\"responseStatus\":[],\"responseDetails\":\"SECRET-FIXTURE\"}",
                "{\"responseStatus\":403,\"responseDetails\":\"SECRET-FIXTURE\"}",
                "{\"responseStatus\":200,\"quotaFinished\":true,\"responseData\":{\"translatedText\":\"SECRET-FIXTURE\"}}",
                "{\"responseStatus\":200,\"responseData\":{\"translatedText\":\"MYMEMORY WARNING: SECRET-FIXTURE\"}}" })
            {
                var handler = new Handler { Reply = (request, token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) }) };
                using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, handler))
                {
                    try { Translate(service, "Sun"); throw new Exception("invalid response accepted"); }
                    catch (InvalidOperationException error) { Assert(!error.Message.Contains("SECRET-FIXTURE"), "server content not leaked"); }
                    Assert(handler.Calls == 1, "no retry or other provider");
                }
            }
        });
        test("MyMemory interruption and preflight failures retain all-or-nothing behavior", () =>
        {
            var handler = new Handler { Reply = async (request, token) => { await Task.Delay(Timeout.Infinite, token); return Response("unused"); } };
            using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, handler))
            using (var cancel = new CancellationTokenSource())
            {
                var work = service.TranslateAsync("Sun", "en", "de", cancel.Token); cancel.Cancel();
                bool cancelled = false; try { work.GetAwaiter().GetResult(); } catch (OperationCanceledException) { cancelled = true; }
                Assert(cancelled && handler.Calls == 1, "active network request cancelled");
            }
            var preflight = new Handler();
            using (var service = new OpenAiTranslation(new OpenAiSettings { UseMyMemory = true }, preflight))
            {
                Fails(() => Translate(service, "a" + new string('\u0301', 501)));
                Assert(preflight.Calls == 0, "oversized single text element not sent");
                Assert(Translate(service, "V140 {0} 1935") == "V140 {0} 1935" && preflight.Calls == 0, "protected-only text not sent");
            }
        });
    }
}
