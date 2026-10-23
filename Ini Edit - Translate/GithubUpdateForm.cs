using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Iet.Update;

namespace Ini_Edit___Translate
{
    internal static class UpdateText
    {
        internal static string T(string de, string en, string fr)
        {
            string language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            return language == "de" ? de : language == "fr" ? fr : en;
        }
    }

    internal sealed class GithubUpdateForm : Form
    {
        private readonly GithubUpdateClient client;
        private readonly CancellationTokenSource cancel = new CancellationTokenSource();
        private readonly Label status = new Label { Dock = DockStyle.Fill, AutoSize = true };
        private readonly TextBox notes = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
        private readonly ProgressBar progress = new ProgressBar { Dock = DockStyle.Fill };
        private readonly Button install = new Button { Width = 210, Height = 32, Enabled = false };
        private bool busy;
        private bool resourcesDisposed;
        private GithubRelease release;
        internal UpdateJob PreparedJob;

        internal GithubUpdateForm(GithubUpdateClient injectedClient = null)
        {
            client = injectedClient ?? new GithubUpdateClient();
            Text = UpdateText.T("GitHub-Update", "GitHub update", "Mise à jour GitHub");
            ClientSize = new Size(650, 410);
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 4 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            install.Text = UpdateText.T("Herunterladen und installieren", "Download and install", "Télécharger et installer");
            var close = new Button { Width = 210, Height = 32, Text = UpdateText.T("Schließen / Abbrechen", "Close / cancel", "Fermer / Annuler") };
            close.Click += (sender, args) => Close();
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(close); buttons.Controls.Add(install);
            layout.Controls.Add(status); layout.Controls.Add(notes); layout.Controls.Add(progress); layout.Controls.Add(buttons);
            Controls.Add(layout);
            install.Click += async (sender, args) =>
            {
                busy = true; install.Enabled = false;
                try
                {
                    status.Text = UpdateText.T("Download und Paketprüfung …", "Downloading and checking package …", "Téléchargement et vérification …");
                    string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "UGSo", "IET2026", "updates", Guid.NewGuid().ToString("N"));
                    var job = await client.Download(release, directory, cancel.Token, new Progress<int>(value => { if (!IsDisposed) progress.Value = value; }));
                    cancel.Token.ThrowIfCancellationRequested();
                    string message = UpdateText.T(
                        "Sind deine geöffneten Dateien gespeichert? Die Anwendung wird für das Update geschlossen. Einstellungen und vorhandene Nachschlagedaten bleiben erhalten. Jetzt installieren?",
                        "Have you saved your open files? The application will close for the update. Settings and existing reference data will be retained. Install now?",
                        "Avez-vous enregistré vos fichiers ouverts ? Le programme va se fermer. Les paramètres et données de référence existantes seront conservés. Installer maintenant ?");
                    if (MessageBox.Show(this, message, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    { Close(); return; }
                    PreparedJob = job;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (OperationCanceledException) { if (!IsDisposed) Close(); }
                catch (Exception)
                {
                    if (!IsDisposed) status.Text = UpdateText.T("Update nicht vorbereitet. Verbindung, Paket oder Schreibrechte prüfen. Die Anwendung bleibt unverändert.",
                        "Update could not be prepared. Check the connection, package or write permissions. The application is unchanged.",
                        "Préparation impossible. Vérifier la connexion, le paquet ou les droits d’écriture. Le programme reste inchangé.");
                }
                finally { busy = false; if (IsDisposed) client.Dispose(); }
            };
            Shown += async (sender, args) =>
            {
                status.Text = UpdateText.T("GitHub wird geprüft …", "Checking GitHub …", "Vérification de GitHub …");
                try
                {
                    release = await client.Latest(cancel.Token);
                    if (IsDisposed) return;
                    Version current = Assembly.GetExecutingAssembly().GetName().Version;
                    status.Text = UpdateText.T("Installiert: ", "Installed: ", "Installée : ") + current +
                        UpdateText.T("   GitHub: ", "   GitHub: ", "   GitHub : ") + release.Version;
                    notes.Text = release.Notes.Replace("\r\n", "\n").Replace("\n", "\r\n");
                    install.Enabled = release.Version > current;
                    if (!install.Enabled) status.Text += Environment.NewLine +
                        UpdateText.T("Kein neueres Release verfügbar.", "No newer release available.", "Aucune version plus récente disponible.");
                }
                catch (OperationCanceledException) { }
                catch (Exception)
                {
                    if (!IsDisposed) status.Text = UpdateText.T("GitHub konnte nicht geprüft werden. Später erneut versuchen.",
                        "Could not check GitHub. Try again later.", "Impossible de vérifier GitHub. Réessayer plus tard.");
                }
            };
            FormClosing += (sender, args) => { cancel.Cancel(); };
            FormClosed += (sender, args) => { if (!busy) client.Dispose(); };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !resourcesDisposed)
            {
                resourcesDisposed = true;
                cancel.Cancel();
                client.Dispose();
                cancel.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
