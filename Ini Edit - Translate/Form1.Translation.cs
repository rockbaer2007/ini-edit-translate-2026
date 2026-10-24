using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    public partial class Form1 : System.Windows.Forms.Form
    {
        private static void BackupInputFile(string path)
        {
            if (!File.Exists(path)) return;
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UGSo", "IET2026", "backups",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            File.Copy(path, Path.Combine(directory, Path.GetFileName(path)));
        }

        private void PrepareIniForWrite(string path)
        {
            BackupInputFile(path);
            var texts = new List<string>();
            Action<Control> collect = null;
            collect = control => { if (control is TextBoxBase) texts.Add(control.Text); foreach (Control child in control.Controls) collect(child); };
            collect(this);
            foreach (var grid in new[] { dataMA, dataSF })
                foreach (DataGridViewRow row in grid.Rows)
                    foreach (DataGridViewCell cell in row.Cells) texts.Add(Convert.ToString(cell.Value));
            EnsureIniEncoding(path, texts);
        }

        internal static void EnsureIniEncoding(string path, IEnumerable<string> texts)
        {
            var ansi = Encoding.GetEncoding(Encoding.Default.CodePage, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            try { foreach (string text in texts) ansi.GetByteCount(text ?? ""); return; }
            catch (EncoderFallbackException) { }
            // Win32 profile APIs retain UTF-16 only when the file already has its BOM.
            string content = "";
            if (File.Exists(path))
                using (var reader = new StreamReader(path, Encoding.Default, true)) content = reader.ReadToEnd();
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, content, Encoding.Unicode);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private OpenAiTranslation openAi;
        private async Task TranslateModelTextsAsync(string from)
        {
            var names = new[] { n_eng_txt, n_ger_txt, n_fra_txt, n_pol_txt };
            var descriptions = new[] { d_eng_txt, d_ger_txt, d_fra_txt, d_pol_txt };
            int source = Array.IndexOf(OpenAiTranslation.Languages, from);
            if (source < 0) throw new ArgumentException(Ui.Text("unsupportedLanguage"));
            string name = names[source].Text, description = descriptions[source].Text;
            var translatedNames = new string[4]; var translatedDescriptions = new string[4];
            bool hasName = ModelNameTranslation.HasDescriptiveWords(name, from), hasDescription = !string.IsNullOrWhiteSpace(description);
            for (int target = 0; target < 4; target++)
            {
                if (target == source) continue;
                if (hasName) translatedNames[target] = ModelNameTranslation.Translate(name, from, OpenAiTranslation.Languages[target]);
                if (hasDescription) translatedDescriptions[target] = await TranslateTextAsync(description, from, OpenAiTranslation.Languages[target]);
            }
            translationCancellation?.Token.ThrowIfCancellationRequested();
            for (int target = 0; target < 4; target++)
            {
                if (target == source) continue;
                if (hasName) names[target].Text = translatedNames[target];
                if (hasDescription) descriptions[target].Text = translatedDescriptions[target];
            }
        }

        private async Task TranslateInstallationSetAsync(int section, string from)
        {
            if (section < 0 || section > 4) throw new ArgumentOutOfRangeException(nameof(section));
            string suffix = section == 0 ? "eep" : "eep" + (section + 1);
            string[] tags = { "eng", "ger", "fra", "pol" }, languages = { "en", "de", "fr", "pl" };
            int source = Array.IndexOf(languages, from);
            if (source < 0 || source > 2) throw new ArgumentException(Ui.Text("unsupportedLanguage"));
            var names = tags.Select(tag => Controls.Find("n_" + tag + "_" + suffix, true).Single()).ToArray();
            var descriptions = tags.Select(tag => Controls.Find("d_" + tag + "_" + suffix, true).Single()).ToArray();
            string name = names[source].Text, description = descriptions[source].Text;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description))
                throw new InvalidOperationException(Ui.Format("missingPair", "Name_" + tags[source].ToUpperInvariant(), "Desc_" + tags[source].ToUpperInvariant()));
            var translatedNames = new string[4]; var translatedDescriptions = new string[4];
            for (int target = 0; target < languages.Length; target++)
            {
                if (target == source) continue;
                translatedNames[target] = await TranslateTextAsync(name, from, languages[target]);
                translatedDescriptions[target] = await TranslateTextAsync(description, from, languages[target]);
            }
            translationCancellation?.Token.ThrowIfCancellationRequested();
            // Publish a complete set together; errors leave the current set untouched.
            for (int target = 0; target < languages.Length; target++)
            {
                if (target == source) continue;
                names[target].Text = translatedNames[target]; descriptions[target].Text = translatedDescriptions[target];
            }
        }

        private CancellationTokenSource translationCancellation;
        private Form translationProgress;
        private bool translationOperationRunning;
        private Action restoreTranslation;

        private void CaptureTranslationState()
        {
            if (restoreTranslation != null) return;
            var snapshot = new TranslationSnapshot(this, dataMA, dataSF);
            restoreTranslation = snapshot.Restore;
        }

        private async Task RunTranslationOperationAsync(Func<Task> operation)
        {
            if (translationOperationRunning) return;
            translationOperationRunning = true;
            restoreTranslation = null;
            translationCancellation = new CancellationTokenSource();
            bool success = false;
            try { await operation(); success = true; }
            catch (OperationCanceledException)
            {
                MessageBox.Show(translationProgress ?? this, Ui.Text("translationCancelled"), Ui.Text("openAiTitle"));
            }
            catch (Exception error)
            {
                using (var dialog = CreateTranslationErrorDialog(Ui.Text("cgStageTranslation") + ": " + OpenAiErrors.SafeMessage(error)))
                    dialog.ShowDialog(translationProgress ?? this);
            }
            finally
            {
                if (!success)
                {
                    restoreTranslation?.Invoke();
                }
                openAi?.Dispose(); openAi = null;
                translationCancellation.Dispose(); translationCancellation = null;
                if (translationProgress != null)
                {
                    var progress = translationProgress; translationProgress = null;
                    progress.Dispose();
                    if (!IsDisposed) { Enabled = true; Activate(); }
                }
                Cursor.Current = Cursors.Default;
                translationOperationRunning = false;
                restoreTranslation = null;
            }
        }

        internal static Form CreateTranslationErrorDialog(string message)
        {
            var dialog = new Form { Text = Ui.Text("openAiTitle"), ClientSize = new Size(650, 240), MinimumSize = new Size(500, 220),
                StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            var text = new TextBox { Text = message, ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
            layout.Controls.Add(text, 0, 0);
            var actions = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var copy = new Button { Text = Ui.Text("cgCopyError"), Dock = DockStyle.Fill };
            copy.Click += (s, e) => { try { Clipboard.SetText(message); } catch { MessageBox.Show(dialog, Ui.Text("cgCopyFailed")); } };
            var close = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Fill };
            actions.Controls.Add(copy, 0, 0); actions.Controls.Add(close, 1, 0); layout.Controls.Add(actions, 0, 1);
            dialog.Controls.Add(layout); dialog.AcceptButton = close; dialog.CancelButton = close;
            return dialog;
        }

        private async Task<string> TranslateTextAsync(string text, string from, string to)
        {
            if (openAi == null)
            {
                // Snapshot after importing the selected file, before the first API call.
                // Restoring a previous file's UI against a newly selected path is unsafe.
                CaptureTranslationState();
                var settings = OpenAiSettings.Load();
                openAi = new OpenAiTranslation(settings);
                translationProgress = new Form { Text = Ui.Text("openAiTitle"), ClientSize = new Size(420, 110),
                    StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false, MinimizeBox = false };
                translationProgress.Controls.Add(new Label { Text = Ui.Text(settings.UseMyMemory ? "mmTranslating" : settings.UseChatGpt ? "cgTranslating" : "translating"),
                    AutoSize = true, MaximumSize = new Size(390, 0), Location = new Point(15, 10) });
                var cancel = new Button { Text = Ui.Text("cancelAction"), Width = 180, Height = 30, Location = new Point(15, 60) };
                cancel.Click += (sender, args) => { translationCancellation.Cancel(); cancel.Enabled = false; };
                translationProgress.FormClosing += (sender, args) =>
                {
                    if (args.CloseReason == CloseReason.UserClosing)
                    {
                        args.Cancel = true;
                        translationCancellation?.Cancel();
                    }
                };
                translationProgress.Controls.Add(cancel);
                translationProgress.Show(this);
                Enabled = false;
            }
            return await openAi.TranslateAsync(text, from, to, translationCancellation.Token);
        }
    }
}
