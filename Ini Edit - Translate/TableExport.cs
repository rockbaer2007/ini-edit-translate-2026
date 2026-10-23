using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    internal static class TableExport
    {
        private static string Cell(object value)
        {
            string text = Convert.ToString(value);
            return text.IndexOfAny(new[] { '\t', '\r', '\n', '"' }) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
        }

        internal static void Write(DataGridView grid, string path)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine(string.Join("\t", grid.Columns.Cast<DataGridViewColumn>().Select(column => Cell(column.HeaderText))));
                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    writer.WriteLine(string.Join("\t", row.Cells.Cast<DataGridViewCell>().Select(cell => Cell(cell.Value))));
                }
            }
        }
    }
}
