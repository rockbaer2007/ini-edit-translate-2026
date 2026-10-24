using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Text;
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
            if (!SaveProgramPath()) return;
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
                SaveProgramPath();
            }
            else if (dialogResult == DialogResult.No)
            {

            }

        }

        internal static string NormalizeProgramPath(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate)) throw new ArgumentException("Empty application path.");
            string path = Environment.ExpandEnvironmentVariables(candidate.Trim().Trim('"'));
            if (!Path.IsPathRooted(path)) throw new ArgumentException("Absolute application path required.");
            string executable = Path.GetFileName(typeof(option).Assembly.Location);
            if (Directory.Exists(path)) path = Path.Combine(path, executable);
            path = Path.GetFullPath(path);
            if (!File.Exists(path) || !string.Equals(Path.GetFileName(path),
                executable, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Application executable not found.");
            return path;
        }

        internal static void WriteProgramPath(RegistryKey key, string candidate)
        {
            key.SetValue("Path", NormalizeProgramPath(candidate), RegistryValueKind.String);
        }

        private bool SaveProgramPath()
        {
            string path;
            try { path = NormalizeProgramPath(textBox1.Text); }
            catch (Exception error) when (error is ArgumentException || error is NotSupportedException || error is IOException)
            {
                MessageBox.Show(this, Ui.Text("programPathInvalid"), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\UGSo\\IET2026"))
                {
                    string previous = key.GetValue("Path") as string;
                    if (!string.IsNullOrEmpty(previous) && !string.Equals(previous, path, StringComparison.OrdinalIgnoreCase))
                    {
                        string backup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "UGSo", "IET2026", "backups", "program-path-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N"));
                        Directory.CreateDirectory(backup);
                        File.WriteAllText(Path.Combine(backup, "program-path.txt"), previous, new UTF8Encoding(false));
                    }
                    WriteProgramPath(key, path);
                }
                readkey();
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is System.Security.SecurityException)
            {
                MessageBox.Show(this, Ui.Text("programPathSaveFailed"), Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
