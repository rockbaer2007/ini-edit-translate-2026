using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    public class about : Form
    {
        public about()
        {
            Text = Ui.Text("aboutTitle");
            ClientSize = new Size(540, 260);
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 4 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label { Text = "INI Edit Translate 2026 · " + Assembly.GetExecutingAssembly().GetName().Version,
                AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
            layout.Controls.Add(new Label { Text = Ui.Text("aboutDescription"), AutoSize = true, MaximumSize = new Size(500, 0) });
            var github = new LinkLabel { Text = "GitHub · UGSo Software", AutoSize = true };
            github.LinkClicked += (sender, args) => Process.Start("https://github.com/rockbaer2007/ini-edit-translate-2026");
            layout.Controls.Add(github);
            var close = new Button { Text = "OK", Width = 120, Height = 30, DialogResult = DialogResult.OK };
            layout.Controls.Add(close);
            Controls.Add(layout);
            AcceptButton = CancelButton = close;
        }
    }
}
