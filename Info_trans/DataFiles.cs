using System;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.VisualBasic.FileIO;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Info_trans
{
    internal static class DataFiles
    {
        internal static string Root = AppDomain.CurrentDomain.BaseDirectory;
        internal static string PathFor(string filename) { return Path.Combine(Root, filename); }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string filename, IntPtr reserved, uint flags);
        [DllImport("kernel32.dll")]
        private static extern bool FreeLibrary(IntPtr module);

        internal static string RequireNativeClient()
        {
            string filename = PathFor("fbembed.dll");
            if (!File.Exists(filename)) filename = PathFor("fbclient.dll");
            if (!File.Exists(filename)) throw new FileNotFoundException("Native Firebird client missing.");
            // Validate architecture and native dependencies before the managed provider registers shutdown callbacks.
            IntPtr module = LoadLibraryEx(filename, IntPtr.Zero, 8);
            if (module == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            FreeLibrary(module);
            return filename;
        }

        internal static DataTable ReadTable(string filename)
        {
            if (!File.Exists(PathFor(filename)) && filename.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                filename = Path.ChangeExtension(filename, ".csv");
            using (var parser = new TextFieldParser(PathFor(filename), System.Text.Encoding.UTF8, true))
                return ReadTable(parser, filename);
        }

        internal static DataTable ReadRailwayTable()
        {
            const string filename = "railway-abbreviations.csv";
            if (File.Exists(PathFor(filename))) return ReadTable(filename);
            using (var stream = typeof(DataFiles).Assembly.GetManifestResourceStream("IET.RailwayAbbreviations.csv"))
            using (var parser = new TextFieldParser(stream ?? throw new InvalidDataException("Railway reference resource missing."), System.Text.Encoding.UTF8, true))
                return ReadTable(parser, filename);
        }

        private static DataTable ReadTable(TextFieldParser parser, string filename)
        {
            var table = new DataTable();
            {
                parser.SetDelimiters(";");
                parser.HasFieldsEnclosedInQuotes = true;
                parser.TrimWhiteSpace = false;
                var columns = parser.ReadFields();
                if (columns == null) return table;
                // The original technical glossary starts with a title, then its actual header.
                if (filename.Equals("twb.csv", StringComparison.OrdinalIgnoreCase)) columns = parser.ReadFields();
                if (filename.Equals("ws_D_E.csv", StringComparison.OrdinalIgnoreCase))
                {
                    table.Columns.Add("Deutsch");
                    table.Columns.Add("English");
                    table.Rows.Add(columns);
                }
                else
                foreach (var column in columns) table.Columns.Add(column);
                while (!parser.EndOfData) table.Rows.Add(parser.ReadFields());
            }
            return table;
        }

        internal static DataTable ReadDictionary()
        {
            // Keep the original DAT/DE-EN fallback for existing installations.
            // The reviewed three-language file has an explicit CSV header.
            return File.Exists(PathFor("ws_D_E_F.csv"))
                ? ReadTable("ws_D_E_F.csv") : ReadTable("ws_D_E.dat");
        }

        internal static string Text(string de, string en, string fr)
        {
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "de": return de;
                case "fr": return fr;
                default: return en;
            }
        }

        internal static string BackupDatabase()
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "UGSo", "IET2026", "backups", DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            string backup = Path.Combine(directory, "IET2.FDB");
            File.Copy(PathFor("IET2.FDB"), backup, false);
            return backup;
        }
    }
}
