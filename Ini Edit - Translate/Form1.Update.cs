using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Iet.Update;

namespace Ini_Edit___Translate
{
    public partial class Form1 : System.Windows.Forms.Form
    {
        private bool updateShutdown;
        private void InitializeGithubUpdate()
        {
            var item = new ToolStripMenuItem(UpdateText.T("Nach Updates suchen …", "Check for updates …", "Rechercher des mises à jour …"))
                { Name = "githubUpdateMenuItem" };
            aboutToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
            aboutToolStripMenuItem.DropDownItems.Add(item);
            item.Click += (sender, args) =>
            {
                if (translationOperationRunning) return;
                using (var dialog = new GithubUpdateForm())
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK || dialog.PreparedJob == null) return;
                    try
                    {
                        var job = dialog.PreparedJob;
                        job.InstallDirectory = Application.StartupPath;
                        job.BackupDirectory = Path.Combine(Path.GetDirectoryName(job.ZipPath), "backup");
                        job.Language = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                        using (var current = Process.GetCurrentProcess())
                        {
                            job.ProcessId = current.Id;
                            job.ProcessStartTicks = current.StartTime.ToUniversalTime().Ticks;
                        }
                        Properties.Settings.Default.Save();
                        string userBackup = Path.Combine(Path.GetDirectoryName(job.ZipPath), "user-settings");
                        Directory.CreateDirectory(userBackup);
                        string configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
                        if (File.Exists(configuration)) File.Copy(configuration, Path.Combine(userBackup, "user.config"));
                        string openAi = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UGSo", "IET2026", "openai.json");
                        if (File.Exists(openAi)) File.Copy(openAi, Path.Combine(userBackup, "openai.json"));
                        string updater = Path.Combine(Path.GetDirectoryName(job.ZipPath), "IET.Updater.exe");
                        File.Copy(Path.Combine(Application.StartupPath, "IET.Updater.exe"), updater);
                        string jobFile = Path.Combine(Path.GetDirectoryName(job.ZipPath), "job.json");
                        UpdateCore.WriteJob(jobFile, job);
                        Process.Start(new ProcessStartInfo(updater, "\"" + jobFile + "\"") { UseShellExecute = true });
                        updateShutdown = true;
                        Close();
                    }
                    catch (Exception)
                    {
                        updateShutdown = false;
                        MessageBox.Show(UpdateText.T("Installation konnte nicht gestartet werden. Die Anwendung bleibt geöffnet.",
                            "Could not start installation. The application remains open.",
                            "Impossible de démarrer l’installation. Le programme reste ouvert."));
                    }
                }
            };
        }
    }

}
