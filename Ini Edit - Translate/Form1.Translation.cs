using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.IO;
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

        private OpenAiTranslation openAi;
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
                MessageBox.Show(translationProgress ?? this, Ui.Text("translationCancelled"), "OpenAI");
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
            var dialog = new Form { Text = "OpenAI", ClientSize = new Size(650, 240), MinimumSize = new Size(500, 220),
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
                translationProgress.Controls.Add(new Label { Text = Ui.Text(settings.UseChatGpt ? "cgTranslating" : "translating"),
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
