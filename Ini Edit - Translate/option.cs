using Microsoft.Win32;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    public partial class option : Form
    {
        public option()
        {
            InitializeComponent();
            var openAiButton = new Button { Text = Ui.Text("openAiTitle"), Dock = DockStyle.Bottom, Height = 34 };
            openAiButton.Click += (sender, args) =>
            {
                try { using (var dialog = new OpenAiSettingsForm()) dialog.ShowDialog(this); }
                catch (Exception) { MessageBox.Show(this, Ui.Text("apiReadError"), "OpenAI"); }
            };
            Controls.Add(openAiButton);
            ClientSize = new Size(ClientSize.Width, ClientSize.Height + 44);
            readkey();
            if (Properties.Settings.Default.read_only == true)
            {

            }
            else
            {
                checkBox2.Checked = true;
            }

            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.culture_l))
            {
                radioButton4.Checked = true;
            }
            else if (Properties.Settings.Default.culture_l == "fr")
            {
                radioButton1.Checked = true;
            }
            else if (Properties.Settings.Default.culture_l == "en")
            {
                radioButton2.Checked = true;
            }
            else if (Properties.Settings.Default.culture_l == "de")
            {
                radioButton3.Checked = true;
            }
        }
        private void readkey()
        {
            using (RegistryKey myKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\UGSo\\IET2026"))
                textBox1.Text = myKey?.GetValue("Path") as string ?? Application.ExecutablePath;
        }
        private void Button1_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.Save();
            MessageBox.Show(Ui.Text("restartRequired"), "Info");
            Application.Restart();

        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.culture_l = "fr";
        }

        private void RadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.culture_l = "en";
        }

        private void RadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.culture_l = "de";
        }

        private void RadioButton4_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.culture_l = null;
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                Properties.Settings.Default.msg_on = true;
                checkBox1.ForeColor = Color.Blue;
            }
            else
            {
                Properties.Settings.Default.msg_on = false;
                checkBox1.ForeColor = Color.Red;
            }
        }

        private void CheckBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked == true)
            {
                Properties.Settings.Default.read_only = false;
                Properties.Settings.Default.Save();
            }
            else
            {
                Properties.Settings.Default.read_only = true;
                Properties.Settings.Default.Save();
            }
        }

private void Option_Load(object sender, EventArgs e)
        {
            {
                checkBox1.Checked = Properties.Settings.Default.msg_on;

                if (checkBox1.Checked == true)
                {
                    checkBox1.ForeColor = Color.Blue;
                }
                else
                {
                    checkBox1.ForeColor = Color.Red;
                }
            }



        }

        private void Button3_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show(Properties.Resources.quest2, Properties.Resources.quest1, MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                RegistryKey newKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\UGSo\\IET2026");
                newKey.SetValue("Path", Application.ExecutablePath);
            }
            else if (dialogResult == DialogResult.No)
            {

            }

        }
    }
}
