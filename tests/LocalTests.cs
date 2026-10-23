using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Ini_Edit___Translate;
using Newtonsoft.Json.Linq;

internal static class LocalTests
{
    private static int passed;
    private const string FakeKey = "local-mock-key-not-a-real-credential";
    private sealed class Handler : HttpMessageHandler
    {
        internal int Calls;
        internal JObject Body;
        internal Func<CancellationToken, Task<HttpResponseMessage>> Reply;
        internal Handler(string body, HttpStatusCode status = HttpStatusCode.OK)
        { Reply = token => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) }); }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Assert(request.RequestUri.AbsoluteUri == "https://api.openai.com/v1/responses", "endpoint");
            Assert(request.Headers.Authorization.Scheme == "Bearer" && request.Headers.Authorization.Parameter == FakeKey, "authorization");
            Body = JObject.Parse(await request.Content.ReadAsStringAsync());
            Assert((bool)Body["store"] == false && (string)Body["text"]["format"]["type"] == "json_schema", "request contract");
            return await Reply(token);
        }
    }
    private static OpenAiSettings Settings() => new OpenAiSettings { ProtectedKey = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(FakeKey), null, DataProtectionScope.CurrentUser)) };
    private static string Response(string de = "Sonne {0}", string fr = "Soleil {0}", string pl = "Słońce {0}") =>
        new JObject { ["status"] = "completed", ["output"] = new JArray(
            new JObject { ["type"] = "reasoning" },
            new JObject { ["type"] = "message", ["content"] = new JArray(new JObject { ["type"] = "output_text",
                ["text"] = new JObject { ["de"] = de, ["fr"] = fr, ["pl"] = pl }.ToString() }) }) }.ToString();
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Test(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    private static void Fails(Action action)
    {
        bool failed = false;
        try { action(); } catch (Exception) { failed = true; }
        Assert(failed, "Expected failure");
    }
    private static string Translate(OpenAiTranslation service, string source = "Sun {0}", string from = "en", string to = "de") =>
        service.TranslateAsync(source, from, to, CancellationToken.None).GetAwaiter().GetResult();

    [STAThread]
    private static int Main(string[] args)
    {
        try
        {
            Test("three target languages, one request, unchanged source", () =>
            {
                var handler = new Handler(Response());
                using (var service = new OpenAiTranslation(Settings(), handler))
                {
                    Assert(Translate(service) == "Sonne {0}", "DE");
                    Assert(Translate(service, to: "fr") == "Soleil {0}", "FR");
                    Assert(Translate(service, to: "pl") == "Słońce {0}", "PL");
                    Assert(handler.Calls == 1 && (string)handler.Body["input"] == "Sun {0}", "cache/source");
                }
            });
            Test("empty and same-language texts do not call network", () =>
            {
                var handler = new Handler(Response());
                using (var service = new OpenAiTranslation(Settings(), handler))
                {
                    Assert(Translate(service, "  ") == "  ", "whitespace");
                    Assert(Translate(service, "original", to: "en") == "original", "same language");
                    Assert(handler.Calls == 0, "unexpected request");
                }
            });
            Test("source text and language separate cache entries", () =>
            {
                var handler = new Handler(Response());
                using (var service = new OpenAiTranslation(Settings(), handler))
                {
                    Translate(service); Translate(service, "Moon {0}"); Assert(handler.Calls == 2, "cache collision");
                    handler.Reply = token => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(
                        new JObject { ["status"] = "completed", ["output"] = new JArray(new JObject { ["type"] = "message", ["content"] = new JArray(new JObject
                        { ["type"] = "output_text", ["text"] = new JObject { ["en"] = "Sun {0}", ["fr"] = "Soleil {0}", ["pl"] = "Słońce {0}" }.ToString() }) }) }.ToString()) });
                    Assert(Translate(service, "Sun {0}", "de", "en") == "Sun {0}" && handler.Calls == 3, "source language cache collision");
                }
            });
            Test("caches never survive operation/service", () =>
            {
                var a = new Handler(Response()); var b = new Handler(Response());
                using (var service = new OpenAiTranslation(Settings(), a)) Translate(service);
                using (var service = new OpenAiTranslation(Settings(), b)) Translate(service);
                Assert(a.Calls == 1 && b.Calls == 1, "shared cache");
            });
            foreach (var body in new[] { "not JSON", "{\"status\":\"incomplete\",\"output\":[]}",
                "{\"status\":\"completed\",\"output\":[]}", Response(fr: ""), Response(de: "Sonne"),
                Response(de: "Sonne {0} {0}"),
                "{\"status\":\"completed\",\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"refusal\"}]}]}" })
            {
                Test("reject invalid/incomplete/refused response " + passed, () =>
                {
                    using (var service = new OpenAiTranslation(Settings(), new Handler(body))) Fails(() => Translate(service));
                });
            }
            foreach (var status in new[] { HttpStatusCode.Unauthorized, (HttpStatusCode)429, HttpStatusCode.InternalServerError, HttpStatusCode.Redirect })
                Test("reject HTTP " + (int)status + " without exposing server contents", () =>
                {
                    using (var service = new OpenAiTranslation(Settings(), new Handler("private server details", status)))
                    {
                        try { Translate(service); throw new Exception("Expected HTTP failure"); }
                        catch (InvalidOperationException error) { Assert(!error.Message.Contains("private server details") && !error.Message.Contains(FakeKey), "secret leaked"); }
                    }
                });
            Test("cancellation interrupts pending request", () =>
            {
                var handler = new Handler(Response());
                handler.Reply = async token => { await Task.Delay(Timeout.Infinite, token); return null; };
                using (var cancellation = new CancellationTokenSource(50))
                using (var service = new OpenAiTranslation(Settings(), handler))
                    Fails(() => service.TranslateAsync("Sun {0}", "en", "de", cancellation.Token).GetAwaiter().GetResult());
            });
            Test("pre-cancellation performs no request", () =>
            {
                var handler = new Handler(Response());
                using (var cancellation = new CancellationTokenSource())
                using (var service = new OpenAiTranslation(Settings(), handler))
                {
                    cancellation.Cancel();
                    Fails(() => service.TranslateAsync("Sun {0}", "en", "de", cancellation.Token).GetAwaiter().GetResult());
                    Assert(handler.Calls == 0, "sent cancelled request");
                }
            });
            Test("missing key fails before network", () =>
            {
                string a = Environment.GetEnvironmentVariable("OPENAI_API_KEY"), b = Environment.GetEnvironmentVariable("IET_OPENAI_API_KEY");
                try
                {
                    Environment.SetEnvironmentVariable("OPENAI_API_KEY", null); Environment.SetEnvironmentVariable("IET_OPENAI_API_KEY", null);
                    var handler = new Handler(Response());
                    using (var service = new OpenAiTranslation(new OpenAiSettings(), handler)) Fails(() => Translate(service));
                    Assert(handler.Calls == 0, "request without key");
                }
                finally { Environment.SetEnvironmentVariable("OPENAI_API_KEY", a); Environment.SetEnvironmentVariable("IET_OPENAI_API_KEY", b); }
            });
            Test("DPAPI round trip", () => Assert(Settings().GetKey() == FakeKey && !Settings().ProtectedKey.Contains(FakeKey), "key encryption"));
            Test("unsupported language and oversized input", () =>
            {
                using (var service = new OpenAiTranslation(Settings(), new Handler(Response())))
                {
                    Fails(() => Translate(service, from: "invalid"));
                    Fails(() => Translate(service, new string('a', 24001)));
                }
            });
            Test("format placeholders and markup preserved", () =>
            {
                OpenAiTranslation.ValidateTokens("A [e] %s {0} <b>\\n", "B [e] %s {0} <b>\\n");
                Fails(() => OpenAiTranslation.ValidateTokens("A [e] %s", "B [e] %d"));
            });
            Test("table export retains final row, Unicode and escaping", () =>
            {
                string file = Path.GetTempFileName();
                try
                {
                    using (var grid = new DataGridView { AllowUserToAddRows = false })
                    {
                        grid.Columns.Add("a", "Name"); grid.Rows.Add("erste"); grid.Rows.Add("Łódź – été\t\"letzte\"");
                        TableExport.Write(grid, file);
                        string text = File.ReadAllText(file, Encoding.UTF8);
                        Assert(text.Contains("erste") && text.Contains("Łódź – été") && text.Contains("\"\"letzte\"\""), "data loss");
                    }
                }
                finally { File.Delete(file); }
            });
            Test("rollback restores text, rich text and table rows", () =>
            {
                using (var form = new Form()) using (var text = new TextBox { Text = "Original" })
                using (var rich = new RichTextBox { Text = "Änderung" }) using (var grid = new DataGridView { AllowUserToAddRows = false })
                {
                    form.Controls.Add(text); form.Controls.Add(rich); form.Controls.Add(grid);
                    grid.Columns.Add("a", "Name"); grid.Rows.Add("vorher");
                    var snapshot = new TranslationSnapshot(form, grid);
                    text.Text = "partial"; rich.Text = "partial"; grid.Rows.Clear(); grid.Rows.Add("partial");
                    snapshot.Restore();
                    Assert(text.Text == "Original" && rich.Text == "Änderung" && (string)grid.Rows[0].Cells[0].Value == "vorher", "rollback lost originals");
                }
            });
            Test("settings dialog fits 100 and 150 percent scale", () =>
            {
                foreach (float scale in new[] { 1f, 1.5f }) using (var form = new OpenAiSettingsForm())
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-20000, -20000);
                    form.ShowInTaskbar = false;
                    form.Scale(new SizeF(scale, scale)); form.Show(); form.PerformLayout(); form.Refresh();
                    foreach (Control root in form.Controls) Assert(form.ClientRectangle.Contains(root.Bounds), "dialog overflow");
                    if (args.Length > 0 && scale == 1f)
                        using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height)); bitmap.Save(args[0]); }
                    form.Close();
                }
            });
            foreach (string language in new[] { "de", "en", "fr" })
            {
                Test("complete runtime translations " + language, () =>
                {
                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                    var resources = Ini_Edit___Translate.Properties.Resources.ResourceManager.GetResourceSet(CultureInfo.CurrentUICulture, true, false);
                    Assert(resources != null, "missing locale resources");
                    foreach (DictionaryEntry item in resources)
                        if (item.Value is string value) Assert(!value.Contains("\ufffd"), "broken text: " + item.Key);
                    Assert(Ui.Text("openAiTitle").Length > 0 && Ui.Text("saveAction").Length > 0, "missing runtime label");
                    Assert(!Ui.Format("missingPair", "Name_GER", "Desc_GER").Contains("{0}"), "unexpanded placeholder");
                    Assert(!Ui.Text("tssl2").Contains("Google"), "obsolete provider status");
                });
                Test("editor labels and update removal " + language, () =>
                {
                    using (var form = new Form1())
                    {
                        // Render only: never invoke startup, file import, network or settings-save handlers.
                        var load = typeof(Form1).GetMethod("Form1_Load", BindingFlags.Instance | BindingFlags.NonPublic);
                        form.Load -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), form, load);
                        var closing = typeof(Form1).GetMethod("Form1_FormClosing", BindingFlags.Instance | BindingFlags.NonPublic);
                        form.FormClosing -= (FormClosingEventHandler)Delegate.CreateDelegate(typeof(FormClosingEventHandler), form, closing);
                        form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false;
                        form.Show(); form.PerformLayout(); form.Refresh();
                        var all = Descendants(form).ToArray();
                        Assert(all.All(control => !control.Text.Contains("\ufffd")), "broken editor label");
                        Assert(!all.Any(control => control.Name == "groupBox3"), "legacy provider panel");
                        var menus = all.OfType<MenuStrip>().SelectMany(menu => menu.Items.Cast<ToolStripItem>()).OfType<ToolStripMenuItem>();
                        Assert(!menus.SelectMany(menu => menu.DropDownItems.Cast<ToolStripItem>()).Any(item => item.Name.IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0), "update action remains");
                        if (args.Length > 0)
                        {
                            string file = Path.Combine(Path.GetDirectoryName(args[0]), "editor-" + language + ".png");
                            using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height)); bitmap.Save(file); }
                        }
                        form.Close();
                    }
                });
            }
            Console.WriteLine(passed + " local checks passed; zero live API calls.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static System.Collections.Generic.IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }
}
