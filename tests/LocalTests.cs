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
            Test("German is default; English and French select the matching source controls", () =>
            {
                Assert(Ui.ResolveLanguage(null) == "de" && Ui.ResolveLanguage("") == "de" && Ui.ResolveLanguage("invalid") == "de", "default German");
                var previous = CultureInfo.CurrentUICulture;
                var context = SynchronizationContext.Current;
                try
                {
                    foreach (string language in new[] { "de", "en", "fr" })
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(language);
                        using (var form = new Form1())
                        {
                            int number = language == "de" ? 4 : language == "en" ? 3 : 5;
                            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                            Assert(((RadioButton)typeof(Form1).GetField("radioButton" + number, flags).GetValue(form)).Checked, "matching source: " + language);
                        }
                    }
                }
                finally { CultureInfo.CurrentUICulture = previous; SynchronizationContext.SetSynchronizationContext(context); }
            });
            Test("INI Unicode text survives save and reload without replacing Polish characters", () =>
            {
                string path = Path.Combine(Path.GetTempPath(), "iet-unicode-" + Guid.NewGuid().ToString("N") + ".ini");
                try
                {
                    File.WriteAllText(path, "[FileInfo]\r\nOriginal=Großdiesellokomotive\r\n", Encoding.Default);
                    byte[] bytes = File.ReadAllBytes(path);
                    Form1.EnsureIniEncoding(path, new[] { "English" });
                    Assert(bytes.SequenceEqual(File.ReadAllBytes(path)), "ANSI unchanged when representable");
                    Form1.EnsureIniEncoding(path, new[] { "Duża lokomotywa" });
                    var ini = new UnicodeIniFile(path); ini["FileInfo"]["Description_POL"] = "Duża lokomotywa";
                    var loaded = new UnicodeIniFile(path);
                    Assert(loaded["FileInfo"]["Description_POL"] == "Duża lokomotywa" && loaded["FileInfo"]["Original"] == "Großdiesellokomotive", "Unicode and original fields retained");
                }
                finally { if (File.Exists(path)) File.Delete(path); }
            });
            Test("last folder restores after restart, respects opt-out and retains unavailable paths", () =>
            {
                var settings = Ini_Edit___Translate.Properties.Settings.Default;
                bool previous = settings.rememberLastFolder; string previousPath = settings.pathdir;
                var context = SynchronizationContext.Current;
                string directory = Path.Combine(Path.GetTempPath(), "iet-last-folder-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory);
                try
                {
                    settings.rememberLastFolder = true; settings.pathdir = directory;
                    for (int restart = 0; restart < 2; restart++)
                    using (var form = new Form1())
                    {
                        var tree = Descendants(form).OfType<DirectoryTreeViewOnDemand>().Single();
                        Assert(tree.Nodes.Count == 1 && tree.Nodes[0].Text == directory, "folder restored on restart");
                        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        Assert(((OpenFileDialog)typeof(Form1).GetField("dlgOpen", flags).GetValue(form)).InitialDirectory == directory, "file picker restored");
                        Assert(((OpenFileDialog)typeof(Form1).GetField("dlgOpen_eep", flags).GetValue(form)).InitialDirectory == directory, "installation picker restored");
                        Assert(((Control)typeof(Form1).GetField("n_ger_txt", flags).GetValue(form)).Text == "", "no automatic file opening");
                    }
                    settings.rememberLastFolder = false;
                    using (var form = new Form1()) Assert(Descendants(form).OfType<DirectoryTreeViewOnDemand>().Single().Nodes.Count == 0, "opt-out skips restoration");
                    settings.rememberLastFolder = true; settings.pathdir = Path.Combine(directory, "missing");
                    using (var form = new Form1())
                    {
                        Assert(Descendants(form).OfType<DirectoryTreeViewOnDemand>().Single().Nodes.Count == 0, "unavailable directory not loaded");
                        Assert(Descendants(form).OfType<Label>().Any(label => label.Text == Ui.Text("lastFolderUnavailable")), "unavailable directory explained");
                        Assert(settings.pathdir.EndsWith("missing"), "temporarily unavailable path retained");
                    }
                }
                finally { settings.rememberLastFolder = previous; settings.pathdir = previousPath; SynchronizationContext.SetSynchronizationContext(context); Directory.Delete(directory); }
            });
            Test("last folder preference persists independently of executable version", () =>
            {
                string file = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
                byte[] previous = File.Exists(file) ? File.ReadAllBytes(file) : null;
                try
                {
                    var saved = new Ini_Edit___Translate.Properties.Settings { rememberLastFolder = true, pathdir = @"C:\EEP\Modelle" };
                    saved.Save();
                    var reloaded = new Ini_Edit___Translate.Properties.Settings();
                    Assert(reloaded.rememberLastFolder && reloaded.pathdir == @"C:\EEP\Modelle", "saved preference and directory reload together");
                }
                finally
                {
                    if (previous == null) { if (File.Exists(file)) File.Delete(file); }
                    else File.WriteAllBytes(file, previous);
                }
            });
            Test("last-folder option renders and toggles in DE EN FR at 100 and 150 percent", () =>
            {
                var settings = Ini_Edit___Translate.Properties.Settings.Default;
                bool remember = settings.rememberLastFolder, readOnly = settings.read_only, message = settings.msg_on;
                string language = settings.culture_l; var culture = CultureInfo.CurrentUICulture; var context = SynchronizationContext.Current;
                try
                {
                    settings.read_only = true;
                    foreach (string locale in new[] { "de", "en", "fr" })
                    foreach (float scale in new[] { 1f, 1.5f })
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(locale);
                        using (var form = new Ini_Edit___Translate.option())
                        {
                            form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false;
                            form.Scale(new SizeF(scale, scale)); form.Show(); form.PerformLayout();
                            var control = Descendants(form).OfType<CheckBox>().Single(box => box.Name == "rememberLastFolder");
                            Assert(control.Text == Ui.Text("rememberLastFolder") && form.ClientRectangle.Contains(control.Bounds), "localized option fits");
                            Assert(!form.Controls.OfType<Button>().Any(button => button.Bounds.IntersectsWith(control.Bounds)), "option does not overlap buttons");
                            control.Checked = true; Assert(settings.rememberLastFolder, "enable bound");
                            control.Checked = false; Assert(!settings.rememberLastFolder, "disable bound");
                            if (args.Length > 0) using (var bitmap = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                                bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0]), "folder-settings-" + locale + "-" + (int)(scale * 100) + ".png"));
                            }
                            form.Close();
                        }
                    }
                }
                finally { settings.rememberLastFolder = remember; settings.read_only = readOnly; settings.msg_on = message; settings.culture_l = language; CultureInfo.CurrentUICulture = culture; SynchronizationContext.SetSynchronizationContext(context); }
            });
            Test("shipped application configuration loads all user and application settings", () =>
            {
                var map = new System.Configuration.ExeConfigurationFileMap { ExeConfigFilename = typeof(Form1).Assembly.Location + ".config" };
                var configuration = System.Configuration.ConfigurationManager.OpenMappedExeConfiguration(map, System.Configuration.ConfigurationUserLevel.None);
                var user = (System.Configuration.ClientSettingsSection)configuration.GetSection("userSettings/Ini_Edit___Translate.Properties.Settings");
                var application = (System.Configuration.ClientSettingsSection)configuration.GetSection("applicationSettings/Ini_Edit___Translate.Properties.Settings");
                Assert(user.Settings.Count > 1 && user.Settings.Get("rememberLastFolder").Value.ValueXml.InnerText == "False", "new and existing user settings deserialize together");
                Assert(application.Settings.Count > 0, "application settings deserialize");
            });
            Test("folder selection shows an INI hint in DE EN FR without clearing data or invoking translation", () =>
            {
                var settings = Ini_Edit___Translate.Properties.Settings.Default;
                bool readOnly = settings.read_only; string originalFile = settings.pathfile;
                var culture = CultureInfo.CurrentUICulture; var context = SynchronizationContext.Current;
                string directory = Path.Combine(Path.GetTempPath(), "iet-folder-click-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(directory); Directory.CreateDirectory(Path.Combine(directory, "folder.ini"));
                File.WriteAllText(Path.Combine(directory, "notes.txt"), "untouched");
                try
                {
                    foreach (string language in new[] { "de", "en", "fr" })
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(language); settings.read_only = false;
                        using (var form = new Form1())
                        using (var tree = new TreeView())
                        {
                            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                            var name = (Control)typeof(Form1).GetField("n_ger_txt", flags).GetValue(form); name.Text = "loaded model";
                            var preview = (RichTextBox)typeof(Form1).GetField("richTextBox1", flags).GetValue(form); preview.Text = "loaded source";
                            var grid = (DataGridView)typeof(Form1).GetField("dataMA", flags).GetValue(form); grid.Rows.Add("1", "drive", "Antrieb", "axe", "napęd");
                            var root = tree.Nodes.Add(directory);
                            foreach (TreeNode node in new[] { root, root.Nodes.Add("folder.ini"), root.Nodes.Add("notes.txt"), root.Nodes.Add("missing.ini") })
                            {
                                string hint = null; SynchronizationContext.SetSynchronizationContext(null);
                                form.OpenSelectedTreeNodeAsync(node, message => hint = message).GetAwaiter().GetResult();
                                Assert(hint == Ui.Text("chooseIniFile"), "localized folder/invalid-selection hint: " + language);
                                Assert(name.Text == "loaded model" && preview.Text == "loaded source" && grid.Rows.Count == 1, "loaded data retained");
                                Assert(settings.pathfile == originalFile, "selected file unchanged");
                                Assert(!(bool)typeof(Form1).GetField("translationOperationRunning", flags).GetValue(form), "no translation operation started");
                            }
                        }
                    }
                }
                finally { settings.read_only = readOnly; settings.pathfile = originalFile; CultureInfo.CurrentUICulture = culture; SynchronizationContext.SetSynchronizationContext(context); Directory.Delete(directory, true); }
            });
            Test("INI selection uses the selected node and accepts uppercase extensions", () =>
            {
                var settings = Ini_Edit___Translate.Properties.Settings.Default;
                bool readOnly = settings.read_only; string originalFile = settings.pathfile;
                var context = SynchronizationContext.Current;
                string directory = Path.Combine(Path.GetTempPath(), "iet-ini-click-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
                string file = Path.Combine(directory, "selected.INI");
                File.WriteAllText(file, "[FileInfo]\r\nName_ENG=Selected model\r\nName_GER=Selected model\r\nName_FRA=Selected model\r\nName_POL=Selected model\r\nDescription_ENG=Description\r\nDescription_GER=Description\r\nDescription_FRA=Description\r\nDescription_POL=Description\r\nIcon=32\r\nGeneration=0\r\nDeployment_Start=1935\r\nDeployment_End=2014\r\nCountry=D\r\n");
                try
                {
                    settings.read_only = true;
                    using (var form = new Form1()) using (var tree = new TreeView())
                    {
                        var root = tree.Nodes.Add(directory); var node = root.Nodes.Add("selected.INI");
                        SynchronizationContext.SetSynchronizationContext(null);
                        form.OpenSelectedTreeNodeAsync(node, message => { throw new Exception("unexpected hint: " + message); }).GetAwaiter().GetResult();
                        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                        Assert(settings.pathfile == file && ((Control)typeof(Form1).GetField("n_ger_txt", flags).GetValue(form)).Text == "Selected model", "selected node opened without mouse hit testing");
                    }
                }
                finally { settings.read_only = readOnly; settings.pathfile = originalFile; SynchronizationContext.SetSynchronizationContext(context); Directory.Delete(directory, true); }
            });
            Test("model-name descriptive words translate while names codes versions and controls remain literal", () =>
            {
                string source = "Haus Helios Typ4 UD1 (GK3) EEP16.2[e]Winter";
                Assert(ModelNameTranslation.Translate(source, "de", "en") == "House Helios Typ4 UD1 (GK3) EEP16.2[e]Winter", "English descriptive word");
                Assert(ModelNameTranslation.Translate(source, "de", "fr") == "Maison Helios Typ4 UD1 (GK3) EEP16.2[e]Hiver", "French descriptive words");
                Assert(ModelNameTranslation.Translate(source, "de", "pl") == "Dom Helios Typ4 UD1 (GK3) EEP16.2[e]Zima", "Polish descriptive words");
                foreach (string language in new[] { "en", "de", "fr", "pl" })
                    Assert(ModelNameTranslation.Translate("UnknownHelios GK3 Typ4 EEP16.2[e]Name_Ident", "de", language) == "UnknownHelios GK3 Typ4 EEP16.2[e]Name_Ident", "unknown names and identifiers not guessed");
                Assert(ModelNameTranslation.Translate("Maison Helios Typ4 UD1", "fr", "de") == "Haus Helios Typ4 UD1", "French source");
                Assert(ModelNameTranslation.Translate("House Helios Typ4 UD1", "en", "pl") == "Dom Helios Typ4 UD1", "English source");
                Assert(ModelNameTranslation.Translate("Dom Helios Typ4 UD1", "pl", "fr") == "Maison Helios Typ4 UD1", "Polish source");
                Assert(ModelNameTranslation.Translate("HAUS Helios", "de", "en") == "HOUSE Helios", "case retained");
            });
            Test("model names translate and save with empty descriptions without needing a provider", () =>
            {
                string directory = Path.Combine(Path.GetTempPath(), "iet-model-names-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
                string file = Path.Combine(directory, "model.ini");
                File.WriteAllText(file, "[FileInfo]\r\nName_ENG=Haus Helios Typ4 UD1 (GK3) EEP16.2[e]Winter\r\nName_GER=Haus Helios Typ4 UD1 (GK3) EEP16.2[e]Winter\r\nName_FRA=Haus Helios Typ4 UD1 (GK3) EEP16.2[e]Winter\r\nName_POL=Haus Helios Typ4 UD1 (GK3) EEP16.2[e]Winter\r\nIcon=32\r\nGeneration=0\r\nDeployment_Start=1935\r\nDeployment_End=2014\r\nCountry=D\r\nCustomField=preserved\r\n");
                bool readOnly = Ini_Edit___Translate.Properties.Settings.Default.read_only; var context = SynchronizationContext.Current;
                try
                {
                    Ini_Edit___Translate.Properties.Settings.Default.read_only = true;
                    using (var form = new Form1())
                    {
                        var flags = BindingFlags.Instance | BindingFlags.NonPublic; SynchronizationContext.SetSynchronizationContext(null);
                        ((Task)typeof(Form1).GetMethod("inilesen", flags).Invoke(form, new object[] { file })).GetAwaiter().GetResult();
                        ((Task)typeof(Form1).GetMethod("gertrans_n", flags).Invoke(form, null)).GetAwaiter().GetResult();
                        Assert(typeof(Form1).GetField("openAi", flags).GetValue(form) == null, "no provider opened for name-only translation");
                        typeof(Form1).GetMethod("inischreiben2", flags).Invoke(form, new object[] { file });
                    }
                    var saved = new UnicodeIniFile(file);
                    Assert(saved["FileInfo"]["Name_ENG"] == "House Helios Typ4 UD1 (GK3) EEP16.2[e]Winter", "English name saved");
                    Assert(saved["FileInfo"]["Name_FRA"] == "Maison Helios Typ4 UD1 (GK3) EEP16.2[e]Hiver", "French name saved");
                    Assert(saved["FileInfo"]["Name_POL"] == "Dom Helios Typ4 UD1 (GK3) EEP16.2[e]Zima" && saved["FileInfo"]["CustomField"] == "preserved", "Polish and unrelated field saved");
                }
                finally { Ini_Edit___Translate.Properties.Settings.Default.read_only = readOnly; SynchronizationContext.SetSynchronizationContext(context); Directory.Delete(directory, true); }
            });
            Test("failed description translation leaves model names and descriptions together untouched", () =>
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic; var context = SynchronizationContext.Current;
                try
                {
                    using (var form = new Form1())
                    using (var cancel = new CancellationTokenSource())
                    using (var service = new OpenAiTranslation(Settings(), new Handler("failure", HttpStatusCode.ServiceUnavailable)))
                    {
                        foreach (string tag in new[] { "eng", "ger", "fra", "pol" })
                        {
                            ((Control)typeof(Form1).GetField("n_" + tag + "_txt", flags).GetValue(form)).Text = "Haus Helios Typ4 UD1";
                            ((Control)typeof(Form1).GetField("d_" + tag + "_txt", flags).GetValue(form)).Text = "Original description";
                        }
                        typeof(Form1).GetField("openAi", flags).SetValue(form, service); typeof(Form1).GetField("translationCancellation", flags).SetValue(form, cancel);
                        SynchronizationContext.SetSynchronizationContext(null);
                        Fails(() => ((Task)typeof(Form1).GetMethod("gertrans_n", flags).Invoke(form, null)).GetAwaiter().GetResult());
                        foreach (string tag in new[] { "eng", "ger", "fra", "pol" })
                            Assert(((Control)typeof(Form1).GetField("n_" + tag + "_txt", flags).GetValue(form)).Text == "Haus Helios Typ4 UD1", "no partial model name applied");
                    }
                }
                finally { SynchronizationContext.SetSynchronizationContext(context); }
            });
            Test("MyMemory defaults only for new configuration and preserves existing provider choices", () =>
            {
                string directory = Path.Combine(Path.GetTempPath(), "iet-provider-default-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
                string file = Path.Combine(directory, "settings.json");
                try
                {
                    Assert(OpenAiSettings.Load(file).UseMyMemory, "new configuration defaults to translated.net");
                    File.WriteAllText(file, "{\"UseChatGpt\":true,\"ProtectedKey\":\"PRIVATE-FIXTURE\",\"ChatGptModel\":\"model-fixture\"}");
                    string original = File.ReadAllText(file); var existing = OpenAiSettings.Load(file);
                    Assert(!existing.UseMyMemory && existing.UseChatGpt && existing.ProtectedKey == "PRIVATE-FIXTURE", "existing provider and key preserved");
                    Assert(File.ReadAllText(file) == original, "load does not rewrite existing settings");
                }
                finally { Directory.Delete(directory, true); }
            });
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
            Test("unreviewed model identifiers stay protected while all five installation set titles and descriptions translate", () =>
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
                        handler.Reply = token =>
                        {
                            string input = (string)handler.Body["input"];
                            string prefix = input.Contains("_unchanged") || input.StartsWith("set-name-") ? "set-name-" : "description-";
                            var values = new JObject();
                            foreach (var property in ((JObject)handler.Body["text"]["format"]["schema"]["properties"]).Properties()) values[property.Name] = prefix + property.Name + (input.Contains("V140") ? " V140" : "");
                            var answer = JObject.Parse(Response()); answer["output"][1]["content"][0]["text"] = values.ToString();
                            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(answer.ToString()) });
                        };
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
                            Assert(handler.Calls == (source == 0 ? 1 : 2) * (section < 0 ? 1 : 2) && (string)handler.Body["input"] ==
                                (source == 0 ? "original-eng" : "description-en"), "English pivot and cached target languages: " + method);
                            string[] targetTags = { "eng", "ger", "fra", "pol" }, targetLanguages = { "en", "de", "fr", "pl" };
                            for (int target = 0; target < 4; target++)
                            {
                                Assert(((Control)typeof(Form1).GetField("n_" + targetTags[target] + "_" + suffix, flags).GetValue(form)).Text ==
                                    (section < 0 || target == source ? "DRG V140 " + targetTags[target] + "_unchanged" : "set-name-" + targetLanguages[target] + " V140"), "set title/source field mismatch: " + method);
                                Assert(((Control)typeof(Form1).GetField("d_" + targetTags[target] + "_" + suffix, flags).GetValue(form)).Text ==
                                    (target == source ? "original-" + tags[source] : "description-" + targetLanguages[target]), "description field mismatch: " + method);
                            }
                        }
                    }
                }
                finally
                {
                    Ini_Edit___Translate.Properties.Settings.Default.language = oldLanguage;
                    SynchronizationContext.SetSynchronizationContext(originalContext);
                }
            });
            Test("failed installation translation leaves all set names and descriptions untouched", () =>
            {
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var context = SynchronizationContext.Current;
                try
                {
                    using (var form = new Form1())
                    {
                        foreach (string tag in new[] { "eng", "ger", "fra", "pol" })
                        foreach (string prefix in new[] { "n_", "d_" })
                            ((Control)typeof(Form1).GetField(prefix + tag + "_eep2", flags).GetValue(form)).Text = "original-" + prefix + tag;
                        var handler = new Handler(Response());
                        handler.Reply = token => Task.FromResult(new HttpResponseMessage(handler.Calls == 1 ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable) { Content = new StringContent(Response()) });
                        using (var service = new OpenAiTranslation(Settings(), handler))
                        using (var cancellation = new CancellationTokenSource())
                        {
                            typeof(Form1).GetField("openAi", flags).SetValue(form, service);
                            typeof(Form1).GetField("translationCancellation", flags).SetValue(form, cancellation);
                            SynchronizationContext.SetSynchronizationContext(null);
                            Fails(() => ((Task)typeof(Form1).GetMethod("TranslateInstallationSetAsync", flags).Invoke(form, new object[] { 1, "en" })).GetAwaiter().GetResult());
                            foreach (string tag in new[] { "eng", "ger", "fra", "pol" })
                            foreach (string prefix in new[] { "n_", "d_" })
                                Assert(((Control)typeof(Form1).GetField(prefix + tag + "_eep2", flags).GetValue(form)).Text == "original-" + prefix + tag, "partial installation output leaked");
                        }
                    }
                }
                finally { SynchronizationContext.SetSynchronizationContext(context); }
            });
            Test("translation output accepts only complete language JSON with optional single fence", () =>
            {
                var envelope = JObject.Parse(Response());
                string json = (string)envelope["output"][1]["content"][0]["text"];
                foreach (string value in new[] { json, "```json\n" + json + "\n```", "```\r\n" + json + "\r\n```" })
                {
                    envelope["output"][1]["content"][0]["text"] = value;
                    var handler = new Handler(envelope.ToString());
                    using (var service = new OpenAiTranslation(Settings(), handler))
                    {
                        Assert(Translate(service) == "Sonne {0}" && Translate(service, to: "fr") == "Soleil {0}", "validated language output");
                        Assert(((string)handler.Body["instructions"]).Contains("JSON object") && ((string)handler.Body["instructions"]).Contains("de, fr, pl"), "explicit JSON instruction and target keys");
                    }
                }
                foreach (string value in new[] { "SECRET-FIXTURE", "", "Here is your JSON: " + json, "```json\n" + json + "\n``` extra", "{invalid", "{\"de\":\"a\",\"de\":\"b\",\"fr\":\"c\",\"pl\":\"d\"}", "{\"de\":\"a\",\"fr\":\"b\",\"pl\":\"c\",\"extra\":\"d\"}" })
                {
                    try { OpenAiTranslation.ParseTranslationOutput(value, new[] { "de", "fr", "pl" }); throw new Exception("invalid output should fail"); }
                    catch (InvalidOperationException error) { Assert(error.Message.Contains("stage=translation-output") && !error.Message.Contains("SECRET-FIXTURE"), "safe output-stage diagnostic"); }
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
            Test("constructor and EEP version mutations are rejected before applying OpenAI results", () =>
            {
                string source = "Drive (GK3) for EEP16.2[e]Lamp (AG3)[e]eep16.2 (DL1)";
                OpenAiTranslation.ValidateTokens(source, "Antrieb (GK3) für EEP16.2[e]Laterne (AG3)[e]eep16.2 (DL1)");
                foreach (string changed in new[] {
                    source.Replace("(GK3)", "(GK3pressure)"), source.Replace("(GK3)", "GK3"),
                    source.Replace("EEP16.2", "EEP16"), source.Replace("EEP16.2", "eep16.2"),
                    source.Replace("[e]", "\n"), source + "[e]", source.Replace("[e]", "[e]\n"),
                    source.Replace("(GK3)", "(TMP1)").Replace("(AG3)", "(GK3)").Replace("(TMP1)", "(AG3)") })
                    Fails(() => OpenAiTranslation.ValidateTokens(source, changed));
                var handler = new Handler(Response(source.Replace("(GK3)", "(GK3pressure)"), source, source));
                using (var service = new OpenAiTranslation(Settings(), handler)) Fails(() => Translate(service, source));
                Assert(((string)handler.Body["instructions"]).Contains("EEP program line-break control"), "control semantics are explicit");
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
                Test("MyMemory settings render without credentials or network " + language, () =>
                {
                    string scratch = Path.Combine(Path.GetTempPath(), "iet-mm-ui-" + Guid.NewGuid().ToString("N"));
                    try
                    {
                        foreach (float scale in new[] { 1f, 1.5f })
                        using (var form = new OpenAiSettingsForm(new OpenAiSettings { UseMyMemory = true, UseChatGpt = true, ProtectedKey = "must-not-read" }, new ChatGptClient(new ChatGptVault(Path.Combine(scratch, "session.dat")))))
                        {
                            form.StartPosition = FormStartPosition.Manual; form.Location = new Point(-20000, -20000); form.ShowInTaskbar = false;
                            form.Scale(new SizeF(scale, scale)); form.Show(); form.PerformLayout(); form.Refresh();
                            Assert(Descendants(form).OfType<ComboBox>().Any(c => c.Items.Count == 3 && c.SelectedIndex == 2), "MyMemory selected");
                            Assert(Descendants(form).OfType<GroupBox>().Any(g => g.Visible && g.Text == Ui.Text("mmProvider")), "MyMemory settings visible");
                            Assert(!Descendants(form).OfType<GroupBox>().Any(g => g.Visible && (g.Text == Ui.Text("cgProviderPlan") || g.Text == Ui.Text("cgProviderApi"))), "other provider fields hidden");
                            foreach (Control control in form.Controls) Assert(form.ClientRectangle.Contains(control.Bounds), "MyMemory dialog overflow");
                            if (args.Length > 0) using (var bitmap = new Bitmap(form.Width, form.Height))
                            {
                                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                                bitmap.Save(Path.Combine(Path.GetDirectoryName(args[0]), "mymemory-settings-" + language + "-" + (int)(scale * 100) + ".png"));
                            }
                            form.Close();
                        }
                    }
                    finally { if (Directory.Exists(scratch)) Directory.Delete(scratch, true); }
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
                            Assert(Descendants(form).OfType<ComboBox>().Any(c => c.Items.Count == 3 && c.SelectedIndex == 1), "plan selected");
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
            Task.Run(() => MyMemoryTests.Run(Test)).GetAwaiter().GetResult();
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
