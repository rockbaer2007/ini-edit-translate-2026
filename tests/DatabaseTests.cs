using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using FirebirdSql.Data.FirebirdClient;
using Newtonsoft.Json;

internal static class DatabaseTests
{
    private static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
    }

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 2) { Console.Error.WriteLine("Usage: IET.DatabaseTests runtime-directory output-directory"); return 2; }
        string runtime = Path.GetFullPath(args[0]);
        string output = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(output);
        string working = Path.Combine(output, "working-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(working);
        string originalHash = Hash(Path.Combine(runtime, "IET2.FDB"));
        foreach (string file in Directory.GetFiles(runtime, "*", SearchOption.AllDirectories))
        {
            string relative = file.Substring(runtime.Length).TrimStart(Path.DirectorySeparatorChar);
            string destination = Path.Combine(working, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            File.Copy(file, destination);
        }
        Info_trans.DataFiles.Root = working;
        try
        {
            Application.EnableVisualStyles();
            string native = Info_trans.DataFiles.RequireNativeClient();
            var builder = new FbConnectionStringBuilder
            {
                UserID = "SYSDBA", Password = "masterkey", Database = Path.Combine(working, "IET2.FDB"),
                ServerType = FbServerType.Embedded, ClientLibrary = native,
                Dialect = 3, Charset = "UTF8", Pooling = false
            };
            var counts = new Dictionary<string, long>();
            var records = new Dictionary<string, DataTable>();
            string serverVersion;
            using (var connection = new FbConnection(builder.ToString()))
            {
                connection.Open();
                serverVersion = connection.ServerVersion;
                Console.WriteLine("Server: " + connection.ServerVersion);
                foreach (string table in new[] { "ESTATE", "SIGNALS", "ROLLING_STOCK", "TRACK_OBJECT", "LANDSCAPE", "GOODS", "DB_UPDATES" })
                {
                    using (var command = new FbCommand("SELECT COUNT(*) FROM " + table, connection))
                        counts[table] = Convert.ToInt64(command.ExecuteScalar());
                    var snapshot = new DataTable();
                    using (var adapter = new FbDataAdapter("SELECT * FROM " + table, connection)) adapter.Fill(snapshot);
                    records[table] = snapshot;
                    Console.WriteLine(table + ": " + counts[table]);
                }
            }
            foreach (string language in new[] { "de", "en", "fr" })
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(language);
                using (var form = new Info_trans.Form1())
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-20000, -20000);
                    form.ShowInTaskbar = false;
                    form.Show(); form.Refresh();
                    var controls = Descendants(form).ToArray();
                    var grids = controls.OfType<DataGridView>().ToDictionary(grid => grid.Name);
                    var tables = new[] { "ESTATE", "SIGNALS", "ROLLING_STOCK", "TRACK_OBJECT", "LANDSCAPE", "GOODS", "DB_UPDATES" };
                    for (int index = 0; index < tables.Length; index++)
                    {
                        var data = grids["dataGridView" + (index + 1)].DataSource as DataTable;
                        if (data == null || data.Rows.Count != counts[tables[index]])
                            throw new Exception("Form table differs from database: " + tables[index]);
                        var expected = records[tables[index]];
                        if (data.Columns.Count != expected.Columns.Count) throw new Exception("Column count differs");
                        for (int row = 0; row < data.Rows.Count; row++)
                            for (int column = 0; column < data.Columns.Count; column++)
                                if (!Equals(data.Rows[row][column], expected.Rows[row][column]))
                                    throw new Exception("Form value differs from database");
                    }
                    if (!controls.OfType<Button>().Single(button => button.Name == "button2").Enabled)
                        throw new Exception("Database did not load completely");
                    var tabs = controls.OfType<TabControl>().Single();
                    tabs.SelectedIndex = 0;
                    form.Refresh();
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                        bitmap.Save(Path.Combine(output, "database-" + language + ".png"));
                    }
                    tabs.SelectedIndex = 6;
                    form.Refresh();
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                        bitmap.Save(Path.Combine(output, "import-" + language + ".png"));
                    }
                    tabs.SelectedIndex = 9;
                    form.Refresh();
                    var dictionary = grids["dataGridView10"].DataSource as DataTable;
                    if (dictionary == null || dictionary.Rows.Count != 2428 || dictionary.Columns.Count != 3)
                        throw new Exception("Three-language dictionary did not load completely");
                    using (var bitmap = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                        bitmap.Save(Path.Combine(output, "dictionary-" + language + ".png"));
                    }
                    form.Close();
                    Console.WriteLine("PASS all database tables displayed " + language);
                }
            }
            if (Hash(Path.Combine(runtime, "IET2.FDB")) != originalHash) throw new Exception("Source database modified");
            File.WriteAllText(Path.Combine(output, "database-test.json"), JsonConvert.SerializeObject(new
            {
                SourceDatabaseUnchanged = true, OriginalSHA256 = originalHash,
                Process64Bit = Environment.Is64BitProcess, Provider = typeof(FbConnection).Assembly.GetName().Version.ToString(),
                ServerVersion = serverVersion, Counts = counts, Locales = new[] { "de", "en", "fr" }
            }, Formatting.Indented));
            Console.WriteLine("PASS source database unchanged");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control nested in Descendants(child)) yield return nested;
        }
    }
}
