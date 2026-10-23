using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    internal sealed class OpenAiSettingsForm : Form
    {
        internal OpenAiSettingsForm()
        {
            Text = Ui.Text("openAiTitle");
            ClientSize = new Size(580, 270);
            MinimumSize = Size;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.Dpi;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 7 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var settings = OpenAiSettings.Load();
            var model = new TextBox { Dock = DockStyle.Fill, Text = settings.Model };
            var key = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
            layout.Controls.Add(new Label { AutoSize = true, Text = Ui.Text("modelLabel") });
            layout.Controls.Add(model);
            layout.Controls.Add(new Label { AutoSize = true, Text = Ui.Text("apiKeyLabel") });
            layout.Controls.Add(key);
            layout.Controls.Add(new Label { AutoSize = true, MaximumSize = new Size(535, 0), Text = Ui.Text("apiPrivacy") });
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Height = 38 };
            var save = new Button { Text = Ui.Text("saveAction"), Width = 125, Height = 30 };
            var cancel = new Button { Text = Ui.Text("cancelAction"), Width = 125, Height = 30, DialogResult = DialogResult.Cancel };
            save.Click += (sender, args) =>
            {
                try { settings.Model = model.Text; settings.Save(key.Text); DialogResult = DialogResult.OK; Close(); }
                catch (Exception) { MessageBox.Show(this, Ui.Text("apiSaveError"), "OpenAI"); }
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            layout.Controls.Add(buttons);
            Controls.Add(layout);
            AcceptButton = save;
            CancelButton = cancel;
        }
    }
}
