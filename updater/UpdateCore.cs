using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Threading;

namespace Iet.Update
{
    [DataContract]
    internal sealed class UpdateJob
    {
        [DataMember] public string InstallDirectory { get; set; }
        [DataMember] public string ZipPath { get; set; }
        [DataMember] public string Sha256 { get; set; }
        [DataMember] public string Version { get; set; }
        [DataMember] public string BackupDirectory { get; set; }
        [DataMember] public int ProcessId { get; set; }
        [DataMember] public long ProcessStartTicks { get; set; }
        [DataMember] public string Language { get; set; }
    }

    internal static class UpdateCore
    {
        internal const string MainExe = "INI Edit Translate 2026.exe";
        internal static string Hash(string path)
        {
            using (var file = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
        }

        internal static void WriteJob(string path, UpdateJob job)
        {
            using (var file = File.Create(path)) new DataContractJsonSerializer(typeof(UpdateJob)).WriteObject(file, job);
        }

        internal static UpdateJob ReadJob(string path)
        {
            using (var file = File.OpenRead(path)) return (UpdateJob)new DataContractJsonSerializer(typeof(UpdateJob)).ReadObject(file);
        }

        internal static string RelativePath(string path)
        {
            string normalized = path.Replace('\\', '/');
            if (normalized.Length == 0 || normalized.StartsWith("/") || normalized.Contains(":") ||
                normalized.Any(character => char.IsControl(character)) ||
                normalized.Split('/').Any(part => part == "." || part == ".." || part.EndsWith(".") || part.EndsWith(" ")))
                throw new InvalidDataException("Invalid package path.");
            return normalized;
        }

        internal static string Target(string root, string relative)
        {
            string directory = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(Path.Combine(directory, RelativePath(relative).Replace('/', Path.DirectorySeparatorChar)));
            if (!full.StartsWith(directory, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Path outside installation.");
            for (string current = Path.GetDirectoryName(full); current != null; current = Path.GetDirectoryName(current))
            {
                if (Directory.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidDataException("Linked installation directory.");
            }
            if (File.Exists(full) && (File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Linked installation file.");
            return full;
        }

        internal static bool Allowed(string relative)
        {
            string name = relative.ToLowerInvariant();
            if (new[] { MainExe.ToLowerInvariant(), MainExe.ToLowerInvariant() + ".config", "iet.updater.exe",
                "inifile.dll", "newtonsoft.json.dll", "info.rtf", "info.de.rtf", "info.en.rtf", "info.fr.rtf",
                "license", "readme.md", "readme.en.md", "readme.fr.md",
                "docs/dictionary-review.md", "docs/dictionary-review.json" }.Contains(name)) return true;
            foreach (string locale in new[] { "de", "en", "fr" })
            {
                if (name == locale + "/ini edit translate 2026.resources.dll" ||
                    name == "help/" + locale + "/info_trans.resources.dll" ||
                    name == "docs/images/editor-" + locale + ".png") return true;
            }
            if (name.StartsWith("help/licenses/") && !name.Substring(14).Contains("/") && name.EndsWith(".txt")) return true;
            return new[] { "help/info_trans.exe", "help/info_trans.exe.config", "help/firebirdsql.data.firebirdclient.dll",
                "help/iet2.fdb", "help/iconhelp.dat", "help/iconhelp.csv", "help/twb.csv", "help/ws_d_e.csv", "help/ws_d_e_f.csv",
                "help/estate.csv", "help/goods.csv", "help/landscape.csv", "help/stock.csv", "help/signals.csv", "help/object.csv",
                "help/reference-data.md", "help/fbembed.dll", "help/fbclient.dll", "help/ib_util.dll", "help/firebird.conf",
                "help/firebird.msg", "help/icudt30.dll", "help/icuin30.dll", "help/icuuc30.dll", "help/msvcp80.dll",
                "help/msvcr80.dll", "help/microsoft.vc80.crt.manifest", "help/intl/fbintl.dll", "help/intl/fbintl.conf",
                "help/udf/fbudf.dll", "help/udf/fbudf.sql", "help/udf/fbudf.txt", "help/udf/ib_udf.dll",
                "help/udf/ib_udf.sql", "help/udf/ib_udf2.sql" }.Contains(name);
        }

        internal static bool Preserve(string relative)
        {
            string name = relative.ToLowerInvariant();
            return name == "info.rtf" || (name.StartsWith("help/") &&
                (name.EndsWith(".fdb") || name.EndsWith(".csv") || name.EndsWith(".dat") || name.EndsWith(".conf")));
        }

        internal static List<string> Extract(UpdateJob job, string staging, CancellationToken cancel = default(CancellationToken))
        {
            cancel.ThrowIfCancellationRequested();
            if (Hash(job.ZipPath) != job.Sha256) throw new InvalidDataException("Checksum differs.");
            var files = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long total = 0;
            using (var archive = ZipFile.OpenRead(job.ZipPath))
            {
                if (archive.Entries.Count > 2000) throw new InvalidDataException("Too many files.");
                foreach (var entry in archive.Entries)
                {
                    cancel.ThrowIfCancellationRequested();
                    string relative = RelativePath(entry.FullName.TrimEnd('/', '\\'));
                    if (!seen.Add(relative)) throw new InvalidDataException("Duplicate package path.");
                    int kind = (entry.ExternalAttributes >> 16) & 0xf000;
                    if (kind != 0 && kind != 0x8000 && kind != 0x4000) throw new InvalidDataException("Linked archive entry.");
                    if (entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\")) continue;
                    if (!Allowed(relative)) throw new InvalidDataException("Unexpected package file: " + relative);
                    total = checked(total + entry.Length);
                    if (entry.Length > 128L * 1024 * 1024 || total > 512L * 1024 * 1024)
                        throw new InvalidDataException("Package is too large.");
                    string destination = Target(staging, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    using (var input = entry.Open())
                    using (var output = new FileStream(destination, FileMode.CreateNew))
                    {
                        var buffer = new byte[81920];
                        long written = 0;
                        int count;
                        while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            cancel.ThrowIfCancellationRequested();
                            written += count;
                            if (written > entry.Length) throw new InvalidDataException("Invalid file size.");
                            output.Write(buffer, 0, count);
                        }
                        if (written != entry.Length) throw new InvalidDataException("Truncated package.");
                    }
                    files.Add(relative);
                }
            }
            foreach (string required in new[] { MainExe, MainExe + ".config", "IET.Updater.exe",
                "Newtonsoft.Json.dll", "IniFile.dll", "help/Info_trans.exe", "help/FirebirdSql.Data.FirebirdClient.dll",
                "help/fbembed.dll", "help/IET2.FDB" })
                if (!files.Contains(required, StringComparer.OrdinalIgnoreCase)) throw new InvalidDataException("Required file missing.");
            Version expected;
            if (!System.Version.TryParse(job.Version, out expected) ||
                Normalize(FileVersionInfo.GetVersionInfo(Target(staging, MainExe)).FileVersion) != Normalize(job.Version))
                throw new InvalidDataException("Executable version differs from release.");
            return files;
        }

        internal static string Normalize(string value)
        {
            var version = new Version(value);
            return new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision)).ToString();
        }

        private static void Replace(string source, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            string temporary = destination + ".iet-update-" + Guid.NewGuid().ToString("N");
            File.Copy(source, temporary);
            try
            {
                if (File.Exists(destination)) File.Replace(temporary, destination, null);
                else File.Move(temporary, destination);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        // Tests can interrupt a specific write to verify restoration after partial installation.
        internal static void Install(UpdateJob job, Action<int> beforeWrite = null)
        {
            string staging = Path.Combine(Path.GetDirectoryName(job.ZipPath), "extract-" + Guid.NewGuid().ToString("N"));
            var files = Extract(job, staging);
            if (!File.Exists(Target(job.InstallDirectory, MainExe))) throw new InvalidDataException("Installation not found.");
            if (Directory.Exists(job.BackupDirectory)) throw new InvalidDataException("Backup directory already exists.");
            Directory.CreateDirectory(job.BackupDirectory);
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var changed = new List<string>();
            var selected = files.Where(file => !(Preserve(file) && File.Exists(Target(job.InstallDirectory, file)))).ToList();
            foreach (string relative in files.Where(file => Preserve(file) && File.Exists(Target(job.InstallDirectory, file))))
            {
                string backup = Target(job.BackupDirectory, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                using (var source = new FileStream(Target(job.InstallDirectory, relative), FileMode.Open, FileAccess.Read, FileShare.None))
                using (var destination = File.Create(backup)) source.CopyTo(destination);
            }
            // Complete the backup and verify writable destinations before replacing the first file.
            foreach (string relative in selected)
            {
                string destination = Target(job.InstallDirectory, relative);
                if (File.Exists(destination))
                {
                    using (var probe = new FileStream(destination, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                    string backup = Target(job.BackupDirectory, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    File.Copy(destination, backup);
                    existing.Add(relative);
                }
            }
            File.WriteAllLines(Path.Combine(job.BackupDirectory, "files.txt"), selected);
            try
            {
                foreach (string relative in selected)
                {
                    beforeWrite?.Invoke(changed.Count);
                    changed.Add(relative);
                    Replace(Target(staging, relative), Target(job.InstallDirectory, relative));
                }
                File.WriteAllText(Path.Combine(job.BackupDirectory, "result.txt"), "Installed " + job.Version);
            }
            catch (Exception failure)
            {
                var errors = new List<string>();
                foreach (string relative in changed.AsEnumerable().Reverse())
                {
                    try
                    {
                        string destination = Target(job.InstallDirectory, relative);
                        if (existing.Contains(relative)) Replace(Target(job.BackupDirectory, relative), destination);
                        else if (File.Exists(destination)) File.Delete(destination);
                    }
                    catch { errors.Add(relative); }
                }
                File.WriteAllText(Path.Combine(job.BackupDirectory, "result.txt"),
                    errors.Count == 0 ? "Installation failed; previous files restored." : "Restoration incomplete: " + string.Join(", ", errors));
                if (errors.Count != 0) throw new IOException("Restoration incomplete; see backup.", failure);
                throw new IOException("Installation failed; previous files restored.", failure);
            }
        }
    }
}
