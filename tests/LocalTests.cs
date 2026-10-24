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
            Test("model names are never translated in model or installation fields", () =>
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                string[] tags = { "eng", "ger", "fra" }, languages = { "en", "de", "fr" }, selections = { "E", "D", "F" };
                string oldLanguage = Ini_Edit___Translate.Properties.Settings.Default.language;
                var originalContext = SynchronizationContext.Current;
                try
                {
                    for (int source = 0; source < 3; source++)
                    for (int section = -1; section < 5; section++)
                    using (var form = new Form1())
                    {
                        string suffix = section < 0 ? "txt" : section == 0 ? "eep" : "eep" + (section + 1);
                        foreach (string tag in new[] { "eng", "ger", "fra", "pol" })
                        {
                            ((Control)typeof(Form1).GetField("n_" + tag + "_" + suffix, flags).GetValue(form)).Text = "DRG V140 " + tag + "_unchanged";
                            ((Control)typeof(Form1).GetField("d_" + tag + "_" + suffix, flags).GetValue(form)).Text = "original-" + tag;
                        }
                        var translated = new JObject();
                        foreach (string lang in new[] { "en", "de", "fr", "pl" }.Where(l => l != languages[source])) translated[lang] = "description-" + lang;
                        var response = new JObject { ["status"] = "completed", ["output"] = new JArray(new JObject { ["type"] = "message", ["content"] = new JArray(new JObject { ["type"] = "output_text", ["text"] = translated.ToString() }) }) };
                        var handler = new Handler(response.ToString());
                        using (var service = new OpenAiTranslation(Settings(), handler))
                        using (var cancellation = new CancellationTokenSource())
                        {
                            typeof(Form1).GetField("openAi", flags).SetValue(form, service);
                            typeof(Form1).GetField("translationCancellation", flags).SetValue(form, cancellation);
                            Ini_Edit___Translate.Properties.Settings.Default.language = selections[source];
                            string method = section < 0 ? tags[source] + "trans_n" : "eep_trans_" + tags[source] + "_to_" + section;
                            var previousContext = SynchronizationContext.Current;
                            try
                            {
                                SynchronizationContext.SetSynchronizationContext(null);
                                ((Task)typeof(Form1).GetMethod(method, flags).Invoke(form, null)).GetAwaiter().GetResult();
                            }
                            finally { SynchronizationContext.SetSynchronizationContext(previousContext); }
                            Assert(handler.Calls == 1 && (string)handler.Body["input"] == "original-" + tags[source], "only source description sent: " + method);
                            foreach (string tag in new[] { "eng", "ger", "fra", "pol" })
                                Assert(((Control)typeof(Form1).GetField("n_" + tag + "_" + suffix, flags).GetValue(form)).Text == "DRG V140 " + tag + "_unchanged", "name changed: " + method);
                            string[] targetTags = { "eng", "ger", "fra", "pol" }, targetLanguages = { "en", "de", "fr", "pl" };
                            for (int target = 0; target < 4; target++)
                                Assert(((Control)typeof(Form1).GetField("d_" + targetTags[target] + "_" + suffix, flags).GetValue(form)).Text ==
                                    (target == source ? "original-" + tags[source] : "description-" + targetLanguages[target]), "description field mismatch: " + method);
                        }
                    }
                }
                finally
                {
                    Ini_Edit___Translate.Properties.Settings.Default.language = oldLanguage;
                    SynchronizationContext.SetSynchronizationContext(originalContext);
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
                    Assert(Ui.Text("tableFilter").EndsWith("|*.csv"), "invalid localized export filter");
                });
                Test("translation errors are readable and copyable " + language, () =>
                {
                    foreach (float scale in new[] { 1f, 1.5f })
                    using (var form = Form1.CreateTranslationErrorDialog(Ui.Text("cgStageTranslation") + ": " + OpenAiErrors.FromStream("server_error").Message))
                    {
                        form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false;
                        form.Scale(new SizeF(scale, scale)); form.Show(); form.PerformLayout(); form.Refresh();
                        Assert(Descendants(form).OfType<TextBox>().Any(t => t.ReadOnly && t.Multiline && t.Text.Contains("SSE; code=server_error")), "diagnostic text selectable");
                        Assert(Descendants(form).OfType<Button>().Any(b => b.Text == Ui.Text("cgCopyError") && b.Visible), "copy action visible");
                        foreach (Control control in form.Controls) Assert(form.ClientRectangle.Contains(control.Bounds), "error dialog root overflow");
                        if (args.Length > 0)
                            using (var bitmap = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                                bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0]), "translation-error-" + language + "-" + (int)(scale * 100) + ".png"));
                            }
                        form.Close();
                    }
                });
                Test("ChatGPT settings render without live authentication " + language, () =>
                {
                    string scratch = Path.Combine(Path.GetTempPath(), "iet-chatgpt-ui-" + Guid.NewGuid().ToString("N"));
                    try
                    {
                        foreach (float scale in new[] { 1f, 1.5f })
                        using (var form = new OpenAiSettingsForm(new OpenAiSettings { UseChatGpt = true },
                            new ChatGptClient(new ChatGptVault(Path.Combine(scratch, "session.dat")))))
                        {
                            form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false;
                            form.Scale(new SizeF(scale, scale)); form.Show(); form.PerformLayout(); form.Refresh();
                            Assert(Descendants(form).OfType<ComboBox>().Any(c => c.Items.Count == 2 && c.SelectedIndex == 1), "plan selected");
                            Assert(Descendants(form).OfType<GroupBox>().Any(g => g.Visible && g.Text == Ui.Text("cgProviderPlan")), "plan controls visible");
                            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                            Func<CancellationToken, Task> failModels = token =>
                            {
                                typeof(OpenAiSettingsForm).GetField("loginConfirmed", flags).SetValue(form, true);
                                typeof(OpenAiSettingsForm).GetField("operation", flags).SetValue(form, Ui.Text("cgStageModels"));
                                return Task.FromException(new InvalidOperationException(Ui.Text("cgPermission") + "\nHTTP 403; code=subscription_sharing_route_not_supported; request-id=req_fixture123"));
                            };
                            ((Task)typeof(OpenAiSettingsForm).GetMethod("RunAsync", flags).Invoke(form, new object[] { failModels })).GetAwaiter().GetResult();
                            form.PerformLayout();
                            Assert(Descendants(form).OfType<Button>().Any(b => b.Visible && b.Text == Ui.Text("cgCopyError")), "error copy action visible");
                            Assert(Descendants(form).OfType<Label>().Any(l => l.Text.Contains(Ui.Text("cgLoginConfirmed")) && l.Text.Contains(Ui.Text("cgStageModels"))), "confirmed login and failed models are distinct");
                            foreach (Control control in form.Controls) Assert(form.ClientRectangle.Contains(control.Bounds), "dialog root overflow");
                            if (args.Length > 0)
                                using (var bitmap = new Bitmap(form.Width, form.Height))
                                {
                                    form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                                    bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0]), "chatgpt-settings-" + language + "-" + (int)(scale * 100) + ".png"));
                                }
                            form.Close();
                        }
                    }
                    finally { if (Directory.Exists(scratch)) Directory.Delete(scratch, true); }
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
                        var updateItems = menus.SelectMany(menu => menu.DropDownItems.Cast<ToolStripItem>())
                            .Where(item => item.Name.IndexOf("update", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                        Assert(updateItems.Length == 1 && updateItems[0].Name == "githubUpdateMenuItem", "only the new GitHub updater is offered");
                        if (args.Length > 0)
                        {
                            string file = Path.Combine(Path.GetDirectoryName(args[0]), "editor-" + language + ".png");
                            using (var bitmap = new Bitmap(form.Width, form.Height)) { form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height)); bitmap.Save(file); }
                            var editorTabs = all.OfType<TabControl>().Single(tab => tab.Name == "tabControl1");
                            editorTabs.SelectedIndex = 4;
                            form.Refresh();
                            using (var bitmap = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                                bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0]), "installation-" + language + ".png"));
                            }
                        }
                        form.Close();
                    }
                });
            }
            string helperData = Path.Combine(Path.GetTempPath(), "iet-helper-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(helperData);
            string previousRoot = Info_trans.DataFiles.Root;
            try
            {
                Info_trans.DataFiles.Root = helperData;
                Test("helper tables preserve quoted separators, Unicode and whitespace", () =>
                {
                    File.WriteAllText(Path.Combine(helperData, "iconhelp.csv"), "Name;Text\r\n\" A;B \";Été\r\n", new UTF8Encoding(true));
                    var data = Info_trans.DataFiles.ReadTable("iconhelp.dat");
                    Assert(data.Rows.Count == 1 && (string)data.Rows[0][0] == " A;B " && (string)data.Rows[0][1] == "Été", "CSV values");
                });
                Test("helper glossary retains the first actual entry", () =>
                {
                    File.WriteAllText(Path.Combine(helperData, "twb.csv"), "Title;;\r\nDE;FR;EN\r\nA;B;C\r\n");
                    File.WriteAllText(Path.Combine(helperData, "ws_D_E.csv"), "Abbildung;figure\r\n");
                    Assert(Info_trans.DataFiles.ReadTable("twb.dat").Rows.Count == 1, "glossary title skipped");
                    Assert((string)Info_trans.DataFiles.ReadTable("ws_D_E.dat").Rows[0][0] == "Abbildung", "first row retained");
                    Assert(Info_trans.DataFiles.ReadDictionary().Columns.Count == 2, "legacy dictionary fallback");
                });
                Test("three-language dictionary uses header, keeps first entry and prioritizes reviewed CSV", () =>
                {
                    File.WriteAllText(Path.Combine(helperData, "ws_D_E_F.csv"), "Deutsch;English;Français\r\nAbbildung;figure;illustration\r\n\"A;B\";C;été\r\n", new UTF8Encoding(true));
                    var dictionary = Info_trans.DataFiles.ReadDictionary();
                    Assert(dictionary.Columns.Count == 3 && dictionary.Columns[2].ColumnName == "Français", "French header");
                    Assert(dictionary.Rows.Count == 2 && (string)dictionary.Rows[0][0] == "Abbildung", "header not data; first entry retained");
                    Assert((string)dictionary.Rows[1][0] == "A;B" && (string)dictionary.Rows[1][2] == "été", "quoted separator and accents");
                });
                Test("helper loads all supplied reference files with original row counts", () =>
                {
                    Info_trans.DataFiles.Root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "Info_trans", "reference-data"));
                    Assert(Info_trans.DataFiles.ReadTable("iconhelp.dat").Rows.Count == 236, "icon rows");
                    Assert(Info_trans.DataFiles.ReadTable("twb.dat").Rows.Count == 369, "technical glossary rows");
                    Assert(Info_trans.DataFiles.ReadTable("ws_D_E.dat").Rows.Count == 2431, "DE EN glossary rows");
                    var dictionary = Info_trans.DataFiles.ReadDictionary();
                    Assert(dictionary.Rows.Count == 2428 && dictionary.Columns.Count == 3, "full DE EN FR dictionary");
                    Assert(dictionary.Rows.Cast<System.Data.DataRow>().All(row => !string.IsNullOrWhiteSpace((string)row[2])), "French completeness");
                    var entries = dictionary.Rows.Cast<System.Data.DataRow>().ToArray();
                    Assert(entries.Any(row => (string)row[0] == "Getreide" && (string)row[1] == "grain" && (string)row[2] == "céréales"), "grain split");
                    Assert(entries.Any(row => (string)row[0] == "Getriebe" && (string)row[1] == "transmission, gear, gear box, gear"), "gearbox split retains remaining English text");
                    Assert(entries.Any(row => (string)row[0] == "Massenbeförderungsmittel" && (string)row[1] == "public transport") && !entries.Any(row => (string)row[0] == "autom"), "public transport correction");
                    Assert(entries.Any(row => ((string)row[0]).Trim() == "Antrieb" && ((string)row[1]).EndsWith("gears and wheels")), "drive continuation merged");
                    Assert(entries.Any(row => ((string)row[0]).Trim() == "Personenzuglokomotive" && ((string)row[1]).EndsWith("as opposed to express passenger locomotive)")), "passenger locomotive continuation merged");
                    Assert(entries.Any(row => ((string)row[0]).Trim() == "Steigung" && ((string)row[1]).EndsWith("ascending grade")), "gradient continuation merged");
                    Assert(entries.Any(row => ((string)row[0]).Trim() == "Transportüberwachung" && ((string)row[1]).EndsWith("transport monitoring")), "monitoring continuation merged");
                    Assert(entries.Any(row => ((string)row[0]).Trim() == "Leerlok" && (string)row[1] == "locomotive running light"), "removed incomplete explanation");
                    Assert(entries.Any(row => ((string)row[0]).Trim() == "Transport unbegleiteter KV" && (string)row[1] == "(amer.) unaccompanied Combined Transport"), "completed combined transport");
                    Assert(entries.Any(row => (string)row[0] == "Öffentlicher beweglicher Landfunk" && (string)row[1] == "Public Mobile Radio"), "public radio German correction");
                    Assert(entries.Any(row => ((string)row[0]).Trim() == "Radsatz" && (string)row[1] == "bogie, (amer.) wheelset") &&
                        entries.Any(row => ((string)row[0]).Trim() == "Triebkopf" && (string)row[1] == "driving trailer; power car"), "British and American original terms retained");
                    Assert(entries.Any(row => (string)row[0] == "" && ((string)row[1]).StartsWith("Combined Transport")) && !entries.Any(row => ((string)row[0]).StartsWith("Kombiverkehr, Wagen")), "requested German text and separate row removed");
                    Assert(entries.Any(row => (string)row[0] == "Weichenstellbock" && (string)row[1] == "switch stand") && entries.Any(row => (string)row[0] == "Weichenstellen" && (string)row[1] == "switching"), "switch stand and switching split");
                    Assert(entries.Any(row => (string)row[0] == "Bedien- und Anzeigesystem" && (string)row[1] == "control and display system" && (string)row[2] == "système de commande et d'affichage"), "completed control and display translation");
                    Assert(entries.Any(row => ((string)row[0]).Trim() == "Kurbelstange" && (string)row[1] == "connecting rod" && (string)row[2] == "bielle motrice"), "removed second incomplete explanation");
                    Assert(entries.All(row => !string.IsNullOrWhiteSpace((string)row[1])), "English translations complete");
                    Assert(entries.Any(row => (string)row[0] == "Umsatz in Geschäftsverantwortung" && (string)row[1] == "revenue under operational responsibility" && (string)row[2] == "chiffre d'affaires sous responsabilité opérationnelle") &&
                        entries.Any(row => (string)row[0] == "Umsatz, im Namen und auf Rechnung von" && (string)row[1] == "sales, in the name of and on the account of") &&
                        entries.Any(row => (string)row[0] == "In Rechnung gestellt wurde" && (string)row[1] == "Amount billed:"), "three sales and billing entries separated");
                    Assert(Info_trans.DataFiles.ReadTable("object.csv").Rows.Count == 1, "one original workbook entry");
                    Assert(Info_trans.DataFiles.ReadTable("estate.csv").Rows.Count == 0, "empty source stays empty");
                    using (var helper = new Info_trans.Form1())
                    {
                        helper.StartPosition = FormStartPosition.Manual;
                        helper.Location = new Point(-20000, -20000);
                        helper.ShowInTaskbar = false;
                        helper.Show(); helper.Refresh();
                        var grid = Descendants(helper).OfType<DataGridView>().Single(item => item.Name == "dataGridView10");
                        Assert(((System.Data.DataTable)grid.DataSource).Rows.Count == 2428 && grid.Columns.Count == 3, "actual three-language form data bound");
                        Assert(Descendants(helper).OfType<TabPage>().Single(page => page.Name == "tabPage10").Text.Contains("DE/EN/FR"), "three-language tab caption");
                        Descendants(helper).OfType<TabControl>().Single().SelectedTab = (TabPage)grid.Parent;
                        helper.Refresh();
                        if (args.Length > 0)
                        {
                            using (var bitmap = new Bitmap(helper.Width, helper.Height))
                            {
                                helper.DrawToBitmap(bitmap, new Rectangle(0, 0, helper.Width, helper.Height));
                                bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0]), "info-trans.png"));
                            }
                        }
                        helper.Close();
                    }
                    Info_trans.DataFiles.Root = helperData;
                });
                Test("helper preserves unreadable database and disables SQL import", () =>
                {
                    string databasePath = Path.Combine(helperData, "IET2.FDB");
                    File.WriteAllText(databasePath, "synthetic-invalid-database");
                    using (var helper = new Info_trans.Form1())
                    {
                        helper.StartPosition = FormStartPosition.Manual;
                        helper.Location = new Point(-20000, -20000);
                        helper.ShowInTaskbar = false;
                        helper.Show(); helper.Refresh();
                        Assert(!Descendants(helper).OfType<Button>().Single(button => button.Name == "button2").Enabled, "SQL disabled");
                        Assert(File.ReadAllText(databasePath) == "synthetic-invalid-database", "original bytes retained");
                        helper.Close();
                    }
                    File.Delete(databasePath);
                });
                foreach (string language in new[] { "de", "en", "fr" })
                {
                    Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                    Test("helper starts with missing data without crashing " + language, () =>
                    {
                        using (var helper = new Info_trans.Form1())
                        {
                            helper.StartPosition = FormStartPosition.Manual;
                            helper.Location = new Point(-20000, -20000);
                            helper.ShowInTaskbar = false;
                            helper.Show();
                            helper.Refresh();
                            var status = Descendants(helper).OfType<StatusStrip>().Single().Items[0].Text;
                            Assert(status.Contains("estate.csv") && status.Contains("IET2.FDB"), "missing data visible");
                            helper.Close();
                        }
                    });
                }
            }
            finally
            {
                Info_trans.DataFiles.Root = previousRoot;
                Directory.Delete(helperData, true);
            }
            foreach (string language in new[] { "de", "en", "fr" })
            {
            Test("localized information opens independently of current working directory " + language, () =>
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                string previousDirectory = Environment.CurrentDirectory;
                try
                {
                    Environment.CurrentDirectory = Path.GetTempPath();
                    using (var information = new infoscreen())
                    {
                        var text = Descendants(information).OfType<RichTextBox>().Single().Text;
                        Assert(!string.IsNullOrWhiteSpace(text), "RTF content loaded");
                        Assert(File.Exists(infoscreen.InformationPath), "information path beside executable");
                        Assert(infoscreen.InformationPath.EndsWith("Info." + language + ".rtf"), "localized document selected");
                        Assert(text.Contains("OpenAI") && !text.Contains("Google") && !text.Contains("Leo.org"), "current translation instructions");
                        Assert(information.Text == Ui.Text("infoTitle"), "localized information title");
                        if (args.Length > 0)
                        {
                            information.StartPosition = FormStartPosition.Manual;
                            information.Location = new Point(-20000, -20000);
                            information.Show(); information.Refresh();
                            using (var bitmap = new Bitmap(information.Width, information.Height))
                            {
                                information.DrawToBitmap(bitmap, new Rectangle(0, 0, information.Width, information.Height));
                                bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0]), "information-" + language + ".png"));
                            }
                            information.Close();
                        }
                    }
                }
                finally { Environment.CurrentDirectory = previousDirectory; }
            });
            }
            Test("custom application path round-trip and invalid input protection", () =>
            {
                string folder = Path.Combine(Path.GetTempPath(), "iet-path-" + Guid.NewGuid().ToString("N"));
                string keyPath = "SOFTWARE\\UGSo\\IET2026-PathTest-" + Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(folder);
                try
                {
                    string exe = Path.Combine(folder, Path.GetFileName(typeof(option).Assembly.Location));
                    File.WriteAllText(exe, "temporary path fixture");
                    Assert(option.NormalizeProgramPath(folder) == exe, "folder resolves to executable");
                    Assert(option.NormalizeProgramPath("\"" + exe + "\"") == exe, "quoted executable accepted");
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(keyPath))
                    {
                        option.WriteProgramPath(key, exe);
                        Assert((string)key.GetValue("Path") == exe, "entered path saved instead of running executable");
                        Fails(() => option.WriteProgramPath(key, Path.Combine(folder, "missing.exe")));
                        Assert((string)key.GetValue("Path") == exe, "invalid entry retains existing path");
                    }
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath))
                        Assert((string)key.GetValue("Path") == exe, "path survives reopening");
                    Fails(() => option.NormalizeProgramPath(""));
                    Fails(() => option.NormalizeProgramPath("relative.exe"));
                }
                finally
                {
                    Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(keyPath, false);
                    Directory.Delete(folder, true);
                }
            });
            Task.Run(() => ChatGptTests.Run(Test)).GetAwaiter().GetResult();
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
