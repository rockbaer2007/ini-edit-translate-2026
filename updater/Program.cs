using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Iet.Update;
[assembly: System.Reflection.AssemblyVersion("2.1.37.0")]
[assembly: System.Reflection.AssemblyFileVersion("2.1.37.0")]
[assembly: System.Reflection.AssemblyCompany("UGSo-Software by UG1")]

internal static class Program
{
    private static string Text(string language, string de, string en, string fr)
    { return language == "de" ? de : language == "fr" ? fr : en; }
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 1) return;
        UpdateJob job = null;
        try
        {
            job = UpdateCore.ReadJob(args[0]);
            Process running = null;
            try { running = Process.GetProcessById(job.ProcessId); } catch (ArgumentException) { }
            if (running != null)
            {
                using (running)
                {
                    if (running.StartTime.ToUniversalTime().Ticks != job.ProcessStartTicks ||
                        !string.Equals(running.MainModule.FileName, UpdateCore.Target(job.InstallDirectory, UpdateCore.MainExe), StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Process identity differs.");
                    if (!running.WaitForExit(120000)) throw new IOException("Application is still running.");
                }
            }
            UpdateCore.Install(job);
            MessageBox.Show(Text(job.Language, "Update installiert. Die Anwendung wird neu gestartet.",
                "Update installed. The application will restart.", "Mise à jour installée. Le programme va redémarrer."),
                "INI Edit Translate 2026", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Process.Start(new ProcessStartInfo(UpdateCore.Target(job.InstallDirectory, UpdateCore.MainExe))
                { WorkingDirectory = job.InstallDirectory, UseShellExecute = true });
        }
        catch (Exception error)
        {
            string language = job?.Language ?? "en";
            string report = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0])), "update-error.txt");
            try { File.WriteAllText(report, error.ToString()); } catch { }
            MessageBox.Show(Text(language, "Update nicht abgeschlossen. Bitte den Sicherungsbericht prüfen:",
                "Update did not complete. Check the backup report:", "La mise à jour n’a pas abouti. Consulter le rapport de sauvegarde :")
                + Environment.NewLine + report,
                "INI Edit Translate 2026", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
