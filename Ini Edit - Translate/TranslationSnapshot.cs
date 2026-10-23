using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    internal sealed class TranslationSnapshot
    {
        private readonly Dictionary<TextBoxBase, string> texts;
        private readonly Dictionary<DataGridView, object[][]> rows;

        internal TranslationSnapshot(Control root, params DataGridView[] grids)
        {
            texts = Descendants(root).OfType<TextBoxBase>().ToDictionary(control => control,
                control => control is RichTextBox rich ? rich.Rtf : control.Text);
            rows = grids.ToDictionary(grid => grid, grid => grid.Rows.Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow).Select(row => row.Cells.Cast<DataGridViewCell>().Select(cell => cell.Value).ToArray()).ToArray());
        }

        private static IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var descendant in Descendants(child)) yield return descendant;
            }
        }

        internal void Restore()
        {
            foreach (var item in texts)
            {
                if (item.Key is RichTextBox rich) rich.Rtf = item.Value;
                else item.Key.Text = item.Value;
            }
            foreach (var item in rows)
            {
                item.Key.Rows.Clear();
                foreach (var row in item.Value) item.Key.Rows.Add(row);
            }
        }
    }
}
