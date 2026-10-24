using Ini_Edit___Translate.Properties;
using IniFile = Ini_Edit___Translate.UnicodeIniFile;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Ini_Edit___Translate
{
    public partial class Form1 : System.Windows.Forms.Form
    {
        private IniFile SettingsJB;
        private string text;
        private string crea_um;
        private string crea_uo;
        private string cu;
        private string s10 = "de";
        private string s11 = "en";
        private string s12 = "fr";
        private readonly string axis = "MovAxis";
        private readonly string func1 = "Pos";
        private readonly string func2 = "_Fn_Name";
        private readonly string str_eng = "_ENG";
        private readonly string str_ger = "_GER";
        private readonly string str_fra = "_FRA";
        private readonly string str_pol = "_POL";
        private IniFile SettingsJB66;
        private string beta21;
        private IniFile SettingsJB67;
        private decimal upc;
        private string directoryPath;
        private string decc;
        private string extension;
        private int startIndex = 0;
        private int length = 2;
        private readonly int Jahr;
        private string test555;
        private string culture_set;
        private string datpath;
        private string bodyname;
        private RegistryKey rk11;
        private string zwa;
        private readonly ResourceManager rm = new ResourceManager("Ini_Edit___Translate.Properties.Resources", Assembly.GetExecutingAssembly());

        public Form1()
        {
            InitializeComponent();
            InitializeGithubUpdate();
            culture_set = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (culture_set != "en" && culture_set != "fr") culture_set = "de";
            radioButton3.Checked = culture_set == "en";
            radioButton4.Checked = culture_set == "de";
            radioButton5.Checked = culture_set == "fr";

            dataMA.AllowUserToAddRows = false;
            dataSF.AllowUserToAddRows = false;
            numericUpDown1.Maximum = 0;
            panel1.Visible = true;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
            label76.Visible = false;
            numericUpDown1.Visible = false;
            menuStrip1.BackColor = Color.Lime;
            if (Properties.Settings.Default.read_only == true)
            {
                radioButton1.Checked = true;
            }
            else
            {
                radioButton2.Checked = true;
            }
            dataMA.EnableHeadersVisualStyles = false;
            dataSF.EnableHeadersVisualStyles = false;
            dataMA.ColumnHeadersDefaultCellStyle.BackColor = Color.Linen;
            dataSF.ColumnHeadersDefaultCellStyle.BackColor = Color.Linen;
            tabControl1.BackColor = Color.Transparent;
            label19.Text = null;
            RestoreLastFolder();
        }

        internal void RestoreLastFolder()
        {
            string path = Properties.Settings.Default.pathdir;
            bool available = Properties.Settings.Default.rememberLastFolder && !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
            directoryTreeViewOnDemand1.BeginInit();
            if (available)
            {
                directoryTreeViewOnDemand1.EndInit();
                dlgOpen.InitialDirectory = dlgOpen_eep.InitialDirectory = path;
                folderBrowserDialog1.SelectedPath = path;
            }
            directoryTreeViewOnDemand1.Visible = label23.Visible = available;
            label24.Visible = !available;
            if (Properties.Settings.Default.rememberLastFolder && !available && !string.IsNullOrWhiteSpace(path))
                label24.Text = Ui.Text("lastFolderUnavailable");
        }

        private void RememberWorkingDirectory(string file)
        {
            if (!Properties.Settings.Default.rememberLastFolder) return;
            Properties.Settings.Default.pathdir = Path.GetDirectoryName(Path.GetFullPath(file));
            dlgOpen.InitialDirectory = dlgOpen_eep.InitialDirectory = Properties.Settings.Default.pathdir;
            folderBrowserDialog1.SelectedPath = Properties.Settings.Default.pathdir;
            Properties.Settings.Default.Save();
        }

        private void Center_Text()
        {
            Graphics g = CreateGraphics();
            double startingPoint = (Width / 2) - (g.MeasureString(Text.Trim(), Font).Width / 2);
            double widthOfASpace = g.MeasureString(" ", Font).Width;
            string tmp = " ";
            double tmpWidth = 0;
            while ((tmpWidth + widthOfASpace) < startingPoint)
            {
                tmp += " ";
                tmpWidth += widthOfASpace;
            }
            Text = tmp + Text.Trim();
        }

        public class TestColorTable : ProfessionalColorTable
        {
            private readonly Color culoare = Color.Lime;
            private readonly Color culoare1 = Color.Black;
            private readonly Color culoare2 = Color.Lime;
            public override Color MenuStripGradientBegin => culoare;
            public override Color MenuStripGradientEnd => culoare;
            public override Color MenuItemSelected => culoare2;
            public override Color MenuItemBorder => culoare1;
            public override Color MenuItemSelectedGradientBegin => culoare2;
            public override Color MenuItemSelectedGradientEnd => culoare2;
            public override Color MenuItemPressedGradientBegin => culoare2;
            public override Color MenuItemPressedGradientEnd => culoare2;
            public override Color MenuBorder => culoare1;
        }

        public static string GetEncodingBodyName(string filePath)
        {
            using (StreamReader sr = new StreamReader(filePath, true))
            {
                sr.Peek();
                return sr.CurrentEncoding.EncodingName;
            }
        }

        private async void fileOpen_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(async () =>
            {
            directoryTreeViewOnDemand1.Visible = false;
            label23.Visible = false;
            label24.Visible = true;
            btn_def_trl.Enabled = false;
            preview.Enabled = false;
            dataMA.Rows.Clear();
            dataMA.Refresh();
            dataSF.Rows.Clear();
            dataSF.Refresh();
            listBox1.Items.Clear();
            richTextBox1.Clear();
            Stream myStream;
            label21.Text = "";
            if (dlgOpen.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                datpath = dlgOpen.FileName;
                if ((myStream = dlgOpen.OpenFile()) != null)
                {
                    string decc = dlgOpen.FileName;
                    bodyname = GetEncodingBodyName(decc);

                    label21.Text = " " + decc;
                    richTextBox1.LoadFile(decc, RichTextBoxStreamType.PlainText);
                }
                RememberWorkingDirectory(dlgOpen.FileName);
                await inilesen(dlgOpen.FileName);
            }

            });
        }
        private void fileSave_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.par_m == true)
            {
                {
                    PrepareIniForWrite(Properties.Settings.Default.pf_m);
                    SettingsJB67 = new IniFile(Properties.Settings.Default.pf_m);
                    SettingsJB67["FileInfo"]["Name_ENG"] = "\"" + n_eng_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Name_GER"] = "\"" + n_ger_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Name_FRA"] = "\"" + n_fra_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Name_POL"] = "\"" + n_pol_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Description_ENG"] = "\"" + d_eng_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Description_GER"] = "\"" + d_ger_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Description_FRA"] = "\"" + d_fra_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Description_POL"] = "\"" + d_pol_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Icon"] = textB_icon.Text;
                    SettingsJB67["FileInfo"]["Generation"] = textB_gen.Text;
                    SettingsJB67["FileInfo"]["Deployment_Start"] = textB_depl_s.Text;
                    SettingsJB67["FileInfo"]["Deployment_End"] = textB_depl_e.Text;
                    SettingsJB67["FileInfo"]["Country"] = "\"" + textB_count.Text + "\"";
                    SettingsJB67["FileInfo"]["Creator_Model"] = "\"" + crea_um + "\"";
                    SettingsJB67["FileInfo"]["Creator_Original"] = "\"" + crea_uo + "\"";
                    if (checkBox1.Checked == true)
                    {
                        SettingsJB67["FileInfo"]["PDFDoc_ENG"] = "\"" + pdfgertxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_GER"] = "\"" + pdfgertxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_FRA"] = "\"" + pdfgertxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_POL"] = "\"" + pdfgertxt.Text + "\"";
                    }
                    else
                    {
                        SettingsJB67["FileInfo"]["PDFDoc_ENG"] = "\"" + pdfengtxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_GER"] = "\"" + pdfgertxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_FRA"] = "\"" + pdffratxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_POL"] = "\"" + pdfpoltxt.Text + "\"";
                    }
                    {
                        int count = dataMA.Rows.Count;
                        for (int index = 0; index < count; ++index)
                        {
                            string as1 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_eng;
                            string as2 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_ger;
                            string as3 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_fra;
                            string as4 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_pol;

                            string bs1 = dataMA.Rows[index].Cells[1].Value.ToString();
                            string bs2 = dataMA.Rows[index].Cells[2].Value.ToString();
                            string bs3 = dataMA.Rows[index].Cells[3].Value.ToString();
                            string bs4 = dataMA.Rows[index].Cells[4].Value.ToString();

                            SettingsJB67["FileInfo"][as1] = "\"" + bs1 + "\"";
                            SettingsJB67["FileInfo"][as2] = "\"" + bs2 + "\"";
                            SettingsJB67["FileInfo"][as3] = "\"" + bs3 + "\"";
                            SettingsJB67["FileInfo"][as4] = "\"" + bs4 + "\"";
                        }
                    }
                    {
                        int count2 = dataSF.Rows.Count;
                        for (int index = 0; index < count2; ++index)
                        {
                            string as11 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_eng;
                            string as21 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_ger;
                            string as31 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_fra;
                            string as41 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_pol;


                            string bs11 = dataSF.Rows[index].Cells[1].Value.ToString();
                            string bs21 = dataSF.Rows[index].Cells[2].Value.ToString();
                            string bs31 = dataSF.Rows[index].Cells[3].Value.ToString();
                            string bs41 = dataSF.Rows[index].Cells[4].Value.ToString();

                            SettingsJB67["Model_SignalFunc"][as11] = "\"" + bs11 + "\"";
                            SettingsJB67["Model_SignalFunc"][as21] = "\"" + bs21 + "\"";
                            SettingsJB67["Model_SignalFunc"][as31] = "\"" + bs31 + "\"";
                            SettingsJB67["Model_SignalFunc"][as41] = "\"" + bs41 + "\"";
                        }
                    }
                }
                Application.Exit();
            }
            else
            {
                directoryPath = Path.GetDirectoryName(label21.Text);
                dlgSave.FileName = Path.GetFileName(label21.Text);
                dlgSave.InitialDirectory = directoryPath;
                if (dlgSave.ShowDialog() == DialogResult.OK)
                {
                    {
                        PrepareIniForWrite(dlgSave.FileName);
                        SettingsJB67 = new IniFile(dlgSave.FileName);
                        SettingsJB67["FileInfo"]["Name_ENG"] = "\"" + n_eng_txt.Text + "\"";
                        SettingsJB67["FileInfo"]["Name_GER"] = "\"" + n_ger_txt.Text + "\"";
                        SettingsJB67["FileInfo"]["Name_FRA"] = "\"" + n_fra_txt.Text + "\"";
                        SettingsJB67["FileInfo"]["Name_POL"] = "\"" + n_pol_txt.Text + "\"";
                        SettingsJB67["FileInfo"]["Description_ENG"] = "\"" + d_eng_txt.Text + "\"";
                        SettingsJB67["FileInfo"]["Description_GER"] = "\"" + d_ger_txt.Text + "\"";
                        SettingsJB67["FileInfo"]["Description_FRA"] = "\"" + d_fra_txt.Text + "\"";
                        SettingsJB67["FileInfo"]["Description_POL"] = "\"" + d_pol_txt.Text + "\"";
                        SettingsJB67["FileInfo"]["Icon"] = textB_icon.Text;
                        SettingsJB67["FileInfo"]["Generation"] = textB_gen.Text;
                        SettingsJB67["FileInfo"]["Deployment_Start"] = textB_depl_s.Text;
                        SettingsJB67["FileInfo"]["Deployment_End"] = textB_depl_e.Text;
                        SettingsJB67["FileInfo"]["Country"] = "\"" + textB_count.Text + "\"";
                        SettingsJB67["FileInfo"]["Creator_Model"] = "\"" + crea_um + "\"";
                        SettingsJB67["FileInfo"]["Creator_Original"] = "\"" + crea_uo + "\"";
                        if (checkBox1.Checked == true)
                        {
                            SettingsJB67["FileInfo"]["PDFDoc_ENG"] = "\"" + pdfgertxt.Text + "\"";
                            SettingsJB67["FileInfo"]["PDFDoc_GER"] = "\"" + pdfgertxt.Text + "\"";
                            SettingsJB67["FileInfo"]["PDFDoc_FRA"] = "\"" + pdfgertxt.Text + "\"";
                            SettingsJB67["FileInfo"]["PDFDoc_POL"] = "\"" + pdfgertxt.Text + "\"";
                        }
                        else
                        {
                            SettingsJB67["FileInfo"]["PDFDoc_ENG"] = "\"" + pdfengtxt.Text + "\"";
                            SettingsJB67["FileInfo"]["PDFDoc_GER"] = "\"" + pdfgertxt.Text + "\"";
                            SettingsJB67["FileInfo"]["PDFDoc_FRA"] = "\"" + pdffratxt.Text + "\"";
                            SettingsJB67["FileInfo"]["PDFDoc_POL"] = "\"" + pdfpoltxt.Text + "\"";
                        }
                        {
                            int count = dataMA.Rows.Count;
                            for (int index = 0; index < count; ++index)
                            {
                                string as1 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_eng;
                                string as2 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_ger;
                                string as3 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_fra;
                                string as4 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_pol;

                                string bs1 = dataMA.Rows[index].Cells[1].Value.ToString();
                                string bs2 = dataMA.Rows[index].Cells[2].Value.ToString();
                                string bs3 = dataMA.Rows[index].Cells[3].Value.ToString();
                                string bs4 = dataMA.Rows[index].Cells[4].Value.ToString();

                                SettingsJB67["FileInfo"][as1] = "\"" + bs1 + "\"";
                                SettingsJB67["FileInfo"][as2] = "\"" + bs2 + "\"";
                                SettingsJB67["FileInfo"][as3] = "\"" + bs3 + "\"";
                                SettingsJB67["FileInfo"][as4] = "\"" + bs4 + "\"";
                            }
                        }
                        {
                            int count2 = dataSF.Rows.Count;
                            for (int index = 0; index < count2; ++index)
                            {
                                string as11 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_eng;
                                string as21 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_ger;
                                string as31 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_fra;
                                string as41 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_pol;


                                string bs11 = dataSF.Rows[index].Cells[1].Value.ToString();
                                string bs21 = dataSF.Rows[index].Cells[2].Value.ToString();
                                string bs31 = dataSF.Rows[index].Cells[3].Value.ToString();
                                string bs41 = dataSF.Rows[index].Cells[4].Value.ToString();

                                SettingsJB67["Model_SignalFunc"][as11] = "\"" + bs11 + "\"";
                                SettingsJB67["Model_SignalFunc"][as21] = "\"" + bs21 + "\"";
                                SettingsJB67["Model_SignalFunc"][as31] = "\"" + bs31 + "\"";
                                SettingsJB67["Model_SignalFunc"][as41] = "\"" + bs41 + "\"";
                            }
                        }
                    }

                }
            }
            Cursor.Current = Cursors.WaitCursor;
        }

        private void exit_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void inischreiben(string filepath)
        {
            PrepareIniForWrite(filepath);
            SettingsJB66 = new IniFile(filepath);
            SettingsJB66["Install_00"]["Name_ENG"] = "\"" + n_eng_eep.Text + "\"";
            SettingsJB66["Install_00"]["Name_GER"] = "\"" + n_ger_eep.Text + "\"";
            SettingsJB66["Install_00"]["Name_FRA"] = "\"" + n_fra_eep.Text + "\"";
            SettingsJB66["Install_00"]["Name_POL"] = "\"" + n_pol_eep.Text + "\"";
            SettingsJB66["Install_00"]["Desc_ENG"] = "\"" + d_eng_eep.Text + "\"";
            SettingsJB66["Install_00"]["Desc_GER"] = "\"" + d_ger_eep.Text + "\"";
            SettingsJB66["Install_00"]["Desc_FRA"] = "\"" + d_fra_eep.Text + "\"";
            SettingsJB66["Install_00"]["Desc_POL"] = "\"" + d_pol_eep.Text + "\"";
            if (upc == 1 || upc == 2)
            {
                SettingsJB66["Install_01"]["Name_ENG"] = "\"" + n_eng_eep2.Text + "\"";
                SettingsJB66["Install_01"]["Name_GER"] = "\"" + n_ger_eep2.Text + "\"";
                SettingsJB66["Install_01"]["Name_FRA"] = "\"" + n_fra_eep2.Text + "\"";
                SettingsJB66["Install_01"]["Name_POL"] = "\"" + n_pol_eep2.Text + "\"";
                SettingsJB66["Install_01"]["Desc_ENG"] = "\"" + d_eng_eep2.Text + "\"";
                SettingsJB66["Install_01"]["Desc_GER"] = "\"" + d_ger_eep2.Text + "\"";
                SettingsJB66["Install_01"]["Desc_FRA"] = "\"" + d_fra_eep2.Text + "\"";
                SettingsJB66["Install_01"]["Desc_POL"] = "\"" + d_pol_eep2.Text + "\"";
                if (upc == 2 || upc == 3)
                {
                    SettingsJB66["Install_02"]["Name_ENG"] = "\"" + n_eng_eep3.Text + "\"";
                    SettingsJB66["Install_02"]["Name_GER"] = "\"" + n_ger_eep3.Text + "\"";
                    SettingsJB66["Install_02"]["Name_FRA"] = "\"" + n_fra_eep3.Text + "\"";
                    SettingsJB66["Install_02"]["Name_POL"] = "\"" + n_pol_eep3.Text + "\"";
                    SettingsJB66["Install_02"]["Desc_ENG"] = "\"" + d_eng_eep3.Text + "\"";
                    SettingsJB66["Install_02"]["Desc_GER"] = "\"" + d_ger_eep3.Text + "\"";
                    SettingsJB66["Install_02"]["Desc_FRA"] = "\"" + d_fra_eep3.Text + "\"";
                    SettingsJB66["Install_02"]["Desc_POL"] = "\"" + d_pol_eep3.Text + "\"";
                    if (upc == 3 || upc == 4)
                    {
                        SettingsJB66["Install_03"]["Name_ENG"] = "\"" + n_eng_eep4.Text + "\"";
                        SettingsJB66["Install_03"]["Name_GER"] = "\"" + n_ger_eep4.Text + "\"";
                        SettingsJB66["Install_03"]["Name_FRA"] = "\"" + n_fra_eep4.Text + "\"";
                        SettingsJB66["Install_03"]["Name_POL"] = "\"" + n_pol_eep4.Text + "\"";
                        SettingsJB66["Install_03"]["Desc_ENG"] = "\"" + d_eng_eep4.Text + "\"";
                        SettingsJB66["Install_03"]["Desc_GER"] = "\"" + d_ger_eep4.Text + "\"";
                        SettingsJB66["Install_03"]["Desc_FRA"] = "\"" + d_fra_eep4.Text + "\"";
                        SettingsJB66["Install_03"]["Desc_POL"] = "\"" + d_pol_eep4.Text + "\"";
                        if (upc == 4)
                        {
                            SettingsJB66["Install_04"]["Name_ENG"] = "\"" + n_eng_eep5.Text + "\"";
                            SettingsJB66["Install_04"]["Name_GER"] = "\"" + n_ger_eep5.Text + "\"";
                            SettingsJB66["Install_04"]["Name_FRA"] = "\"" + n_fra_eep5.Text + "\"";
                            SettingsJB66["Install_04"]["Name_POL"] = "\"" + n_pol_eep5.Text + "\"";
                            SettingsJB66["Install_04"]["Desc_ENG"] = "\"" + d_eng_eep5.Text + "\"";
                            SettingsJB66["Install_04"]["Desc_GER"] = "\"" + d_ger_eep5.Text + "\"";
                            SettingsJB66["Install_04"]["Desc_FRA"] = "\"" + d_fra_eep5.Text + "\"";
                            SettingsJB66["Install_04"]["Desc_POL"] = "\"" + d_pol_eep5.Text + "\"";
                        }
                    }
                }
            }
        }

        private void inischreiben2(string path)
        {
            PrepareIniForWrite(path);
            {
                {
                    SettingsJB67 = new IniFile(path);
                    SettingsJB67["FileInfo"]["Name_ENG"] = "\"" + n_eng_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Name_GER"] = "\"" + n_ger_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Name_FRA"] = "\"" + n_fra_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Name_POL"] = "\"" + n_pol_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Description_ENG"] = "\"" + d_eng_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Description_GER"] = "\"" + d_ger_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Description_FRA"] = "\"" + d_fra_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Description_POL"] = "\"" + d_pol_txt.Text + "\"";
                    SettingsJB67["FileInfo"]["Icon"] = textB_icon.Text;
                    SettingsJB67["FileInfo"]["Generation"] = textB_gen.Text;
                    SettingsJB67["FileInfo"]["Deployment_Start"] = textB_depl_s.Text;
                    SettingsJB67["FileInfo"]["Deployment_End"] = textB_depl_e.Text;
                    SettingsJB67["FileInfo"]["Country"] = "\"" + textB_count.Text + "\"";
                    SettingsJB67["FileInfo"]["Creator_Model"] = "\"" + crea_um + "\"";
                    SettingsJB67["FileInfo"]["Creator_Original"] = "\"" + crea_uo + "\"";
                    if (checkBox1.Checked == true)
                    {
                        SettingsJB67["FileInfo"]["PDFDoc_ENG"] = "\"" + pdfgertxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_GER"] = "\"" + pdfgertxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_FRA"] = "\"" + pdfgertxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_POL"] = "\"" + pdfgertxt.Text + "\"";
                    }
                    else
                    {
                        SettingsJB67["FileInfo"]["PDFDoc_ENG"] = "\"" + pdfengtxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_GER"] = "\"" + pdfgertxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_FRA"] = "\"" + pdffratxt.Text + "\"";
                        SettingsJB67["FileInfo"]["PDFDoc_POL"] = "\"" + pdfpoltxt.Text + "\"";
                    }
                    {
                        int count = dataMA.Rows.Count;
                        for (int index = 0; index < count; ++index)
                        {
                            string as1 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_eng;
                            string as2 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_ger;
                            string as3 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_fra;
                            string as4 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_pol;

                            string bs1 = dataMA.Rows[index].Cells[1].Value.ToString();
                            string bs2 = dataMA.Rows[index].Cells[2].Value.ToString();
                            string bs3 = dataMA.Rows[index].Cells[3].Value.ToString();
                            string bs4 = dataMA.Rows[index].Cells[4].Value.ToString();

                            SettingsJB67["FileInfo"][as1] = "\"" + bs1 + "\"";
                            SettingsJB67["FileInfo"][as2] = "\"" + bs2 + "\"";
                            SettingsJB67["FileInfo"][as3] = "\"" + bs3 + "\"";
                            SettingsJB67["FileInfo"][as4] = "\"" + bs4 + "\"";
                        }
                    }
                    {
                        int count2 = dataSF.Rows.Count;
                        for (int index = 0; index < count2; ++index)
                        {
                            string as11 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_eng;
                            string as21 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_ger;
                            string as31 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_fra;
                            string as41 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_pol;


                            string bs11 = dataSF.Rows[index].Cells[1].Value.ToString();
                            string bs21 = dataSF.Rows[index].Cells[2].Value.ToString();
                            string bs31 = dataSF.Rows[index].Cells[3].Value.ToString();
                            string bs41 = dataSF.Rows[index].Cells[4].Value.ToString();

                            SettingsJB67["Model_SignalFunc"][as11] = "\"" + bs11 + "\"";
                            SettingsJB67["Model_SignalFunc"][as21] = "\"" + bs21 + "\"";
                            SettingsJB67["Model_SignalFunc"][as31] = "\"" + bs31 + "\"";
                            SettingsJB67["Model_SignalFunc"][as41] = "\"" + bs41 + "\"";
                        }
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (dlgPDF.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pdfengtxt.Text = Path.GetFileName(dlgPDF.FileName);
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            if (dlgPDF.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                pdfgertxt.Text = Path.GetFileName(dlgPDF.FileName);
                if (Properties.Settings.Default.all == true)
                {
                    pdfgertxt.Text = pdfengtxt.Text = pdffratxt.Text = pdfpoltxt.Text = Path.GetFileName(dlgPDF.FileName);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (dlgPDF.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pdffratxt.Text = Path.GetFileName(dlgPDF.FileName);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (dlgPDF.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pdfpoltxt.Text = Path.GetFileName(dlgPDF.FileName);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                Properties.Settings.Default.all = true;
                pdfengtxt.Enabled = false;
                pdffratxt.Enabled = false;
                pdfpoltxt.Enabled = false;
                button1.Enabled = false;
                button3.Enabled = false;
                button4.Enabled = false;
            }
            else
            {
                Properties.Settings.Default.all = false;
                pdfengtxt.Enabled = true;
                pdffratxt.Enabled = true;
                pdfpoltxt.Enabled = true;
                button1.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
            }
        }

        private void icongrafik(int iconr)
        {
            if (iconr == 1 || iconr == 2 || iconr == 3 || iconr == 4 || iconr == 145 || iconr == 146 || iconr == 147 || iconr == 151 || iconr == 152 || iconr == 153)
            {
                MessageBox.Show(Properties.Resources.icon_int, "Info");

            }
            else if (iconr >= 100)
            {
                string iconr2 = "_" + iconr;
                object O = Resources.ResourceManager.GetObject(iconr2);
                iconBox.Image = (Image)O;
            }
            else if (iconr >= 10)
            {
                string iconr2 = "_0" + +iconr;
                object O = Resources.ResourceManager.GetObject(iconr2);
                iconBox.Image = (Image)O;
            }

            else if (iconr >= 5)
            {
                string iconr2 = "_00" + +iconr;
                object O = Resources.ResourceManager.GetObject(iconr2);
                iconBox.Image = (Image)O;
            }
        }

        private async Task inilesen(string filepath)
        {
            string directoryName = Path.GetDirectoryName(filepath);
            string fileName = Path.GetFileNameWithoutExtension(filepath);
            string dmName = Path.GetFileName(fileName + ".3dm");
            {
                DateTime d = DateTime.Now;
                int Tag = d.Day;
                int Monat = d.Month;
                int Jahr = d.Year;
            }
            Cursor.Current = Cursors.WaitCursor;
            SettingsJB = new IniFile(filepath);
            n_eng_txt.Text = SettingsJB["FileInfo"]["Name_ENG"].Replace("\"", "").Replace("_", " ");
            n_ger_txt.Text = SettingsJB["FileInfo"]["Name_GER"].Replace("\"", "").Replace("_", " ");
            n_fra_txt.Text = SettingsJB["FileInfo"]["Name_FRA"].Replace("\"", "").Replace("_", " ");
            n_pol_txt.Text = SettingsJB["FileInfo"]["Name_POL"].Replace("\"", "").Replace("_", " ");
            d_eng_txt.Text = SettingsJB["FileInfo"]["Description_ENG"].Replace("\"", "").Replace("_", " ");
            d_ger_txt.Text = SettingsJB["FileInfo"]["Description_GER"].Replace("\"", "").Replace("_", " ");
            d_fra_txt.Text = SettingsJB["FileInfo"]["Description_FRA"].Replace("\"", "").Replace("_", " ");
            d_pol_txt.Text = SettingsJB["FileInfo"]["Description_POL"].Replace("\"", "").Replace("_", " ");
            pdfengtxt.Text = SettingsJB["FileInfo"]["PdfDoc_ENG"].Replace("\"", "");
            pdfgertxt.Text = SettingsJB["FileInfo"]["PdfDoc_GER"].Replace("\"", "");
            pdffratxt.Text = SettingsJB["FileInfo"]["PdfDoc_FRA"].Replace("\"", "");
            pdfpoltxt.Text = SettingsJB["FileInfo"]["PdfDoc_POL"].Replace("\"", "");
            textB_icon.Text = SettingsJB["FileInfo"]["Icon"];
            int numVal = int.Parse(textB_icon.Text);
            if (numVal == 23)
            {
                MessageBox.Show(Properties.Resources.icon_std, "Info");
            }
            else
            {
                icongrafik(numVal);
            }

            textB_gen.Text = SettingsJB["FileInfo"]["Generation"];
            if (textB_gen.Text == "0")
            {
                textB_depl_s.Text = SettingsJB["FileInfo"]["Deployment_Start"];
                textB_depl_e.Text = SettingsJB["FileInfo"]["Deployment_End"];
            }
            else if (textB_gen.Text == "1")
            {
                textB_depl_s.Text = "1835";
                textB_depl_e.Text = "1920";
            }
            else if (textB_gen.Text == "2")
            {
                textB_depl_s.Text = "1920";
                textB_depl_e.Text = "1950";
            }
            else if (textB_gen.Text == "3")
            {
                textB_depl_s.Text = "1950";
                textB_depl_e.Text = "1970";
            }
            else if (textB_gen.Text == "4")
            {
                textB_depl_s.Text = "1970";
                textB_depl_e.Text = "1990";
            }
            else if (textB_gen.Text == "5")
            {
                textB_depl_s.Text = "1990";
                textB_depl_e.Text = Jahr.ToString();
            }
            else if (textB_gen.Text == "6")
            {
                textB_depl_s.Text = "2007";
                textB_depl_e.Text = Jahr.ToString();
            }
            textB_count.Text = SettingsJB["FileInfo"]["Country"];
            crea_um = SettingsJB["FileInfo"]["Creator_Model"];
            crea_uo = SettingsJB["FileInfo"]["Creator_Original"];
            if (Properties.Settings.Default.read_only == true)
            {
                if (culture_set.Contains("de"))
                {
                    if (string.IsNullOrEmpty(n_ger_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg9"), "Info");
                    }
                }
                else if (culture_set.Contains("en"))
                {
                    if (string.IsNullOrEmpty(n_eng_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg8"), "Info");
                    }
                }
                else if (culture_set.Contains("fr"))
                {
                    if (string.IsNullOrEmpty(n_fra_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg10"), "Info");
                    }
                }
                for (int j = 1; j < 100; j++)
                {
                    string abc = j.ToString("00");
                    string a1 = SettingsJB["Model_SignalFunc"]["Pos" + abc + "_Fn_Name_ENG"].Replace("\"", "").Replace("_", " ");
                    string a2 = SettingsJB["Model_SignalFunc"]["Pos" + abc + "_Fn_Name_GER"].Replace("\"", "").Replace("_", " ");
                    string a3 = SettingsJB["Model_SignalFunc"]["Pos" + abc + "_Fn_Name_FRA"].Replace("\"", "").Replace("_", " ");
                    string a4 = SettingsJB["Model_SignalFunc"]["Pos" + abc + "_Fn_Name_POL"].Replace("\"", "").Replace("_", " ");
                    if (string.IsNullOrEmpty(a1))
                    {

                    }
                    else
                    {
                        dataSF.Rows.Add(abc, a1, a2, a3, a4);
                    }
                }
                for (int i = 1; i < 100; i++)
                {
                    string def = i.ToString();
                    string a1 = SettingsJB["FileInfo"]["MovAxis" + i + "_ENG"].Replace("\"", "").Replace("_", " ");
                    string a2 = SettingsJB["FileInfo"]["MovAxis" + i + "_GER"].Replace("\"", "").Replace("_", " ");
                    string a3 = SettingsJB["FileInfo"]["MovAxis" + i + "_FRA"].Replace("\"", "").Replace("_", " ");
                    string a4 = SettingsJB["FileInfo"]["MovAxis" + i + "_POL"].Replace("\"", "").Replace("_", " ");
                    if (string.IsNullOrEmpty(a1))
                    {

                    }
                    else
                    {

                        dataMA.Rows.Add(def, a1, a2, a3, a4);
                    }
                }
            }
            else
            {
                if (culture_set.Contains("de"))
                {
                    if (string.IsNullOrEmpty(n_ger_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg9"), "Info");
                    }
                    else
                    {
                        await gertrans_n();
                        await gertrans_m();
                        dataSF.SelectAll();
                        dataSF.ClearSelection();
                        await gertrans_s();
                    }
                }
                else if (culture_set.Contains("en"))
                {
                    if (string.IsNullOrEmpty(n_eng_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg8"), "Info");
                    }
                    else
                    {
                        await engtrans_n();
                        await engtrans_m();
                        dataSF.SelectAll();
                        dataSF.ClearSelection();
                        await engtrans_s();
                    }
                }
                else if (culture_set.Contains("fr"))
                {
                    if (string.IsNullOrEmpty(n_fra_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg10"), "Info");
                    }
                    else
                    {
                        await fratrans_n();
                        await fratrans_m();
                        dataSF.SelectAll();
                        dataSF.ClearSelection();
                        await fratrans_s();
                    }
                }

            }
            Cursor.Current = Cursors.Default;
            btn_def_trl.Enabled = true;
            preview.Enabled = true;
            btn_man.Enabled = true;
            radioButton3.Enabled = true;
            radioButton4.Enabled = true;
            radioButton5.Enabled = true;
        }

        private async Task inilesen_auto(string filepath)
        {
            Cursor.Current = Cursors.WaitCursor;
            SettingsJB = new IniFile(filepath);
            n_eng_txt.Text = SettingsJB["FileInfo"]["Name_ENG"].Replace("\"", "").Replace("_", " ");
            n_ger_txt.Text = SettingsJB["FileInfo"]["Name_GER"].Replace("\"", "").Replace("_", " ");
            n_fra_txt.Text = SettingsJB["FileInfo"]["Name_FRA"].Replace("\"", "").Replace("_", " ");
            n_pol_txt.Text = SettingsJB["FileInfo"]["Name_POL"].Replace("\"", "").Replace("_", " ");
            d_eng_txt.Text = SettingsJB["FileInfo"]["Description_ENG"].Replace("\"", "").Replace("_", " ");
            d_ger_txt.Text = SettingsJB["FileInfo"]["Description_GER"].Replace("\"", "").Replace("_", " ");
            d_fra_txt.Text = SettingsJB["FileInfo"]["Description_FRA"].Replace("\"", "").Replace("_", " ");
            d_pol_txt.Text = SettingsJB["FileInfo"]["Description_POL"].Replace("\"", "").Replace("_", " ");
            pdfengtxt.Text = SettingsJB["FileInfo"]["PdfDoc_ENG"].Replace("\"", "");
            pdfgertxt.Text = SettingsJB["FileInfo"]["PdfDoc_GER"].Replace("\"", "");
            pdffratxt.Text = SettingsJB["FileInfo"]["PdfDoc_FRA"].Replace("\"", "");
            pdfpoltxt.Text = SettingsJB["FileInfo"]["PdfDoc_POL"].Replace("\"", "");
            textB_icon.Text = SettingsJB["FileInfo"]["Icon"];
            textB_gen.Text = SettingsJB["FileInfo"]["Generation"];
            textB_depl_s.Text = SettingsJB["FileInfo"]["Deployment_Start"];
            textB_depl_e.Text = SettingsJB["FileInfo"]["Deployment_End"];
            textB_count.Text = SettingsJB["FileInfo"]["Country"];
            crea_um = SettingsJB["FileInfo"]["Creator_Model"];
            crea_uo = SettingsJB["FileInfo"]["Creator_Original"];
            {
                if (Properties.Settings.Default.language == "D")
                {
                    if (string.IsNullOrEmpty(n_ger_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg9"), "Info");
                    }
                    else
                    {
                        await gertrans_n();
                        await gertrans_m();
                        dataSF.SelectAll();
                        dataSF.ClearSelection();
                        await gertrans_s();
                    }
                }
                else if (Properties.Settings.Default.language == "E")
                {
                    if (string.IsNullOrEmpty(n_eng_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg8"), "Info");
                    }
                    else
                    {
                        await engtrans_n();
                        await engtrans_m();
                        dataSF.SelectAll();
                        dataSF.ClearSelection();
                        await engtrans_s();
                    }
                }
                else if (Properties.Settings.Default.language == "F")
                {
                    if (string.IsNullOrEmpty(n_fra_txt.Text))
                    {
                        MessageBox.Show(Ui.Text("msg10"), "Info");
                    }
                    else
                    {
                        await fratrans_n();
                        await fratrans_m();
                        dataSF.SelectAll();
                        dataSF.ClearSelection();
                        await fratrans_s();
                    }
                }
            }
            Cursor.Current = Cursors.Default;
            btn_def_trl.Enabled = true;
            preview.Enabled = true;
            btn_man.Enabled = true;
            radioButton3.Enabled = true;
            radioButton4.Enabled = true;
            radioButton5.Enabled = true;
        }

        private async Task inilesen2(string filepath)
        {
            Cursor.Current = Cursors.WaitCursor;
            panel1.Visible = true;
            panel2.Visible = false;
            panel3.Visible = false;
            panel4.Visible = false;
            panel5.Visible = false;
            SettingsJB66 = new IniFile(filepath);
            n_eng_eep.Text = SettingsJB66["Install_00"]["Name_ENG"].Replace("\"", "").Replace("_", " ");
            n_ger_eep.Text = SettingsJB66["Install_00"]["Name_GER"].Replace("\"", "").Replace("_", " ");
            n_fra_eep.Text = SettingsJB66["Install_00"]["Name_FRA"].Replace("\"", "").Replace("_", " ");
            n_pol_eep.Text = SettingsJB66["Install_00"]["Name_POL"].Replace("\"", "").Replace("_", " ");
            d_eng_eep.Text = SettingsJB66["Install_00"]["Desc_ENG"].Replace("\"", "").Replace("_", " ");
            d_ger_eep.Text = SettingsJB66["Install_00"]["Desc_GER"].Replace("\"", "").Replace("_", " ");
            d_fra_eep.Text = SettingsJB66["Install_00"]["Desc_FRA"].Replace("\"", "").Replace("_", " ");
            d_pol_eep.Text = SettingsJB66["Install_00"]["Desc_POL"].Replace("\"", "").Replace("_", " ");
            label43.Text = SettingsJB66["Install_00"]["Script"].Replace("\"", "");

            n_eng_eep2.Text = SettingsJB66["Install_01"]["Name_ENG"].Replace("\"", "").Replace("_", " ");
            n_ger_eep2.Text = SettingsJB66["Install_01"]["Name_GER"].Replace("\"", "").Replace("_", " ");
            n_fra_eep2.Text = SettingsJB66["Install_01"]["Name_FRA"].Replace("\"", "").Replace("_", " ");
            n_pol_eep2.Text = SettingsJB66["Install_01"]["Name_POL"].Replace("\"", "").Replace("_", " ");
            d_eng_eep2.Text = SettingsJB66["Install_01"]["Desc_ENG"].Replace("\"", "").Replace("_", " ");
            d_ger_eep2.Text = SettingsJB66["Install_01"]["Desc_GER"].Replace("\"", "").Replace("_", " ");
            d_fra_eep2.Text = SettingsJB66["Install_01"]["Desc_FRA"].Replace("\"", "").Replace("_", " ");
            d_pol_eep2.Text = SettingsJB66["Install_01"]["Desc_POL"].Replace("\"", "").Replace("_", " ");
            label42.Text = SettingsJB66["Install_01"]["Script"].Replace("\"", "");
            if (string.IsNullOrEmpty(label42.Text))
            {
                panel1.Visible = true;
                panel2.Visible = false;
                panel3.Visible = false;
                panel4.Visible = false;
                panel5.Visible = false;
                numericUpDown1.Visible = false;
                upc = numericUpDown1.Maximum = 0;
                label76.Visible = false;

            }
            else
            {
                n_eng_eep3.Text = SettingsJB66["Install_02"]["Name_ENG"].Replace("\"", "").Replace("_", " ");
                n_ger_eep3.Text = SettingsJB66["Install_02"]["Name_GER"].Replace("\"", "").Replace("_", " ");
                n_fra_eep3.Text = SettingsJB66["Install_02"]["Name_FRA"].Replace("\"", "").Replace("_", " ");
                n_pol_eep3.Text = SettingsJB66["Install_02"]["Name_POL"].Replace("\"", "").Replace("_", " ");
                d_eng_eep3.Text = SettingsJB66["Install_02"]["Desc_ENG"].Replace("\"", "").Replace("_", " ");
                d_ger_eep3.Text = SettingsJB66["Install_02"]["Desc_GER"].Replace("\"", "").Replace("_", " ");
                d_fra_eep3.Text = SettingsJB66["Install_02"]["Desc_FRA"].Replace("\"", "").Replace("_", " ");
                d_pol_eep3.Text = SettingsJB66["Install_02"]["Desc_POL"].Replace("\"", "").Replace("_", " ");
                label65.Text = SettingsJB66["Install_02"]["Script"].Replace("\"", "");
                if (string.IsNullOrEmpty(label65.Text))
                {
                    panel1.Visible = true;
                    panel2.Visible = false;
                    panel3.Visible = false;
                    panel4.Visible = false;
                    panel5.Visible = false;
                    numericUpDown1.Visible = true;
                    upc = numericUpDown1.Maximum = 1;
                    label76.Visible = true;

                }
                else
                {
                    n_eng_eep4.Text = SettingsJB66["Install_03"]["Name_ENG"].Replace("\"", "").Replace("_", " ");
                    n_ger_eep4.Text = SettingsJB66["Install_03"]["Name_GER"].Replace("\"", "").Replace("_", " ");
                    n_fra_eep4.Text = SettingsJB66["Install_03"]["Name_FRA"].Replace("\"", "").Replace("_", " ");
                    n_pol_eep4.Text = SettingsJB66["Install_03"]["Name_POL"].Replace("\"", "").Replace("_", " ");
                    d_eng_eep4.Text = SettingsJB66["Install_03"]["Desc_ENG"].Replace("\"", "").Replace("_", " ");
                    d_ger_eep4.Text = SettingsJB66["Install_03"]["Desc_GER"].Replace("\"", "").Replace("_", " ");
                    d_fra_eep4.Text = SettingsJB66["Install_03"]["Desc_FRA"].Replace("\"", "").Replace("_", " ");
                    d_pol_eep4.Text = SettingsJB66["Install_03"]["Desc_POL"].Replace("\"", "").Replace("_", " ");
                    label54.Text = SettingsJB66["Install_03"]["Script"].Replace("\"", "");
                    if (string.IsNullOrEmpty(label54.Text))
                    {
                        panel1.Visible = true;
                        panel2.Visible = false;
                        panel3.Visible = false;
                        panel4.Visible = false;
                        panel5.Visible = false;
                        numericUpDown1.Visible = true;
                        upc = numericUpDown1.Maximum = 2;
                        label76.Visible = true;

                    }
                    else
                    {
                        n_eng_eep5.Text = SettingsJB66["Install_04"]["Name_ENG"].Replace("\"", "").Replace("_", " ");
                        n_ger_eep5.Text = SettingsJB66["Install_04"]["Name_GER"].Replace("\"", "").Replace("_", " ");
                        n_fra_eep5.Text = SettingsJB66["Install_04"]["Name_FRA"].Replace("\"", "").Replace("_", " ");
                        n_pol_eep5.Text = SettingsJB66["Install_04"]["Name_POL"].Replace("\"", "").Replace("_", " ");
                        d_eng_eep5.Text = SettingsJB66["Install_04"]["Desc_ENG"].Replace("\"", "").Replace("_", " ");
                        d_ger_eep5.Text = SettingsJB66["Install_04"]["Desc_GER"].Replace("\"", "").Replace("_", " ");
                        d_fra_eep5.Text = SettingsJB66["Install_04"]["Desc_FRA"].Replace("\"", "").Replace("_", " ");
                        d_pol_eep5.Text = SettingsJB66["Install_04"]["Desc_POL"].Replace("\"", "").Replace("_", " ");
                        label77.Text = SettingsJB66["Install_04"]["Script"].Replace("\"", "");
                        if (string.IsNullOrEmpty(label77.Text))
                        {
                            panel1.Visible = true;
                            panel2.Visible = false;
                            panel3.Visible = false;
                            panel4.Visible = false;
                            panel5.Visible = false;
                            numericUpDown1.Visible = true;
                            upc = numericUpDown1.Maximum = 3;
                            label76.Visible = true;

                        }
                    }
                }
            }

            if (Properties.Settings.Default.par_ia == true)
            {
                tabControl1.SelectedTab = tabPage5;
                await trans2();
            }
            Cursor.Current = Cursors.Default;
        }

        private async void btn_def_trl_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(async () =>
            {
            Cursor.Current = Cursors.WaitCursor;
            // OpenAI replaces the old Google option; existing settings remain untouched.
            {
                if (culture_set.Contains("de"))
                {
                    if (string.IsNullOrEmpty(n_ger_txt.Text))
                    {
                        MessageBox.Show(Properties.Resources.msg9, "Info");
                    }
                    else
                    {
                        await gertrans_n();
                    }
                }
                else if (culture_set.Contains("en"))
                {
                    if (string.IsNullOrEmpty(n_eng_txt.Text))
                    {
                        MessageBox.Show(Properties.Resources.msg8, "Info");
                    }
                    else
                    {
                        await engtrans_n();
                    }
                }
                else if (culture_set.Contains("fr"))
                {
                    if (string.IsNullOrEmpty(n_fra_txt.Text))
                    {
                        MessageBox.Show(Properties.Resources.msg10, "Info");
                    }
                    else
                    {
                        await fratrans_n();
                    }
                }
            }
            Cursor.Current = Cursors.Default;

            });
        }
        private bool SubKeyExist(string Subkey)
        {
            RegistryKey myKey = Registry.CurrentUser.OpenSubKey(Subkey);
            if (myKey == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        private async void Form1_Load(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(async () =>
            {
            {
                if (Properties.Settings.Default.culture_l == "de")
                {
                    pictureBox1.Visible = pictureBox10.Visible = true;
                }
                else if (Properties.Settings.Default.culture_l == "fr")
                {
                    pictureBox8.Visible = pictureBox11.Visible = true;
                }
                else if (Properties.Settings.Default.culture_l == "en")
                {
                    pictureBox9.Visible = pictureBox12.Visible = true;
                }
                else if (Properties.Settings.Default.culture_l == null)
                {
                    CultureInfo ci1 = CultureInfo.InstalledUICulture;
                    ci1 = CultureInfo.CurrentUICulture;
                    cu = ci1.ToString();
                    String s2 = cu.Substring(startIndex, length);
                    if (s10 == s2)
                    {
                        pictureBox8.Visible = pictureBox11.Visible = true;
                    }
                    else if (s11 == s2)
                    {
                        pictureBox1.Visible = pictureBox10.Visible = true;
                    }
                    else if (s12 == s2)
                    {
                        pictureBox9.Visible = pictureBox12.Visible = true;
                    }
                }
            }


            bool checkconnection = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            if (checkconnection)
            {
                // ICMP to Google says nothing about OpenAI HTTPS availability.
                // The translation request reports actual endpoint failures.
                bool checkInternet = checkconnection;
                if (checkInternet)
                {
                    label16.Text = Properties.Resources.tssl2;
                    label16.ForeColor = Color.Green;
                }
                else
                {
                    label16.Text = Properties.Resources.tssl1;
                    label16.ForeColor = Color.Blue;
                }
            }
            else
            {
                label16.Text = Properties.Resources.tssl3;
                label16.ForeColor = Color.Red;
            }

            culture_set = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (culture_set != "en" && culture_set != "fr") culture_set = "de";
            Text = "INI Edit Translate 2026";
            button10.Enabled = btn_save.Enabled = true;
            menuStrip1.Renderer = new ToolStripProfessionalRenderer(new TestColorTable());

            if (Properties.Settings.Default.par_i == true)
            {
                Cursor = Cursors.WaitCursor;
                button10.Enabled = false;
                button11.Enabled = false;
                button9.Enabled = false;
                button13.Enabled = false;
                tabPage1.Enabled = false;
                tabPage2.Enabled = false;
                tabPage3.Enabled = false;
                tabPage4.Enabled = false;
                await inilesen2(Properties.Settings.Default.pf_i);
                tabControl1.SelectedTab = tabPage5;
                test555 = directoryPath = Path.GetDirectoryName(Properties.Settings.Default.pf_i);
                dlgSave.FileName = Path.GetFileName(Properties.Settings.Default.pf_i);
                button13.Enabled = false;
                Cursor = Cursors.Default;
            }
            else if (Properties.Settings.Default.par_ia == true)
            {
                Visible = false;
                Cursor = Cursors.WaitCursor;
                await inilesen2(Properties.Settings.Default.pf_ia);
                {
                    inischreiben(Properties.Settings.Default.pf_ia);
                    if (Properties.Settings.Default.msg_on == false)
                    {
                        MessageBox.Show(Ui.Format("translationSaved", Properties.Settings.Default.pf_ia), Ui.Text("automaticTranslation"));
                    }
                    else
                    {

                    }
                    Close();
                }
                Cursor = Cursors.Default;
            }
            else if (Properties.Settings.Default.par_m == true)
            {
                Cursor = Cursors.WaitCursor;
                tabPage5.Enabled = false;
                button9.Enabled = false;
                button11.Enabled = false;
                button10.Enabled = true;
                radioButton2.Checked = true;
                decc = Properties.Settings.Default.pf_m;
                bodyname = GetEncodingBodyName(decc);
                label21.Text = " " + decc;
                extension = Path.GetExtension(decc);
                if (extension == ".ini")
                {
                    richTextBox1.LoadFile(decc, RichTextBoxStreamType.PlainText);
                    await inilesen(decc);
                    Cursor = Cursors.Default;
                }
                else
                {
                    MessageBox.Show(Ui.Text("notIniFile"), "IET");

                }
            }
            else if (Properties.Settings.Default.par_ma == true)
            {
                Visible = false;
                Cursor = Cursors.WaitCursor;
                string decc = Properties.Settings.Default.pf_ma;
                bodyname = GetEncodingBodyName(decc);
                label21.Text = " " + decc;
                extension = Path.GetExtension(decc);
                if (extension == ".ini")
                {
                    Properties.Settings.Default.read_only = false;
                    await inilesen_auto(Properties.Settings.Default.pf_ma);
                    {
                        inischreiben2(Properties.Settings.Default.pf_ma);
                        //{
                        //    string filePath = Properties.Settings.Default.pf_ma;

                        //    StreamReader fileStream = new StreamReader(filePath);
                        //    string fileContent = fileStream.ReadToEnd();
                        //    Encoding fileEncoding = fileStream.CurrentEncoding;
                        //    fileStream.Close();

                        //    Encoding unicode = Encoding.Unicode;

                        //    // Here I'm trying to guess which encoding to use, to save as ANSI,
                        //    Encoding ansi = Encoding.GetEncoding(1252);

                        //    Byte[] utf8Bytes = unicode.GetBytes(fileContent);

                        //    Byte[] ansiBytes = Encoding.Convert(unicode, ansi, utf8Bytes);

                        //    string ansiContent = ansi.GetString(ansiBytes);

                        //    // Now writes the content in ANSI
                        //    StreamWriter ansiWriter = new StreamWriter(Properties.Settings.Default.pf_ma);
                        //    ansiWriter.Write(ansiContent);
                        //    ansiWriter.Close();
                        //}
                        if (Properties.Settings.Default.msg_on == false)
                        {
                            MessageBox.Show(Ui.Format("translationSaved", Properties.Settings.Default.pf_ma), Ui.Text("automaticTranslation"));
                        }
                        else
                        {

                        }
                        Close();
                    }
                    Properties.Settings.Default.read_only = true;
                    Cursor = Cursors.Default;
                }
                else
                {
                    MessageBox.Show(Ui.Text("notIniFile"), "IET");
                }
            }
            else
            {

            }
            Center_Text();

            });
        }



        private void preview_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            tabControl1.SelectedTab = tabPage3;
            listBox1.Items.Clear();
            listBox1.Items.Add("[FileInfo]");
            listBox1.Items.Add("Name_ENG \t=" + " " + "\"" + n_eng_txt.Text + "\"");
            listBox1.Items.Add("Name_GER \t=" + " " + "\"" + n_ger_txt.Text + "\"");
            listBox1.Items.Add("Name_FRA \t=" + " " + "\"" + n_fra_txt.Text + "\"");
            listBox1.Items.Add("Name_POL \t=" + " " + "\"" + n_pol_txt.Text + "\"");
            listBox1.Items.Add("Icon \t\t=" + " " + textB_icon.Text);
            listBox1.Items.Add("Generation \t=" + " " + textB_gen.Text);
            listBox1.Items.Add("Deployment_Start \t= " + " " + textB_depl_s.Text);
            listBox1.Items.Add("Deployment_End \t= " + " " + textB_depl_e.Text);
            listBox1.Items.Add("Country \t\t= " + " " + textB_count.Text);
            listBox1.Items.Add("Creator_Model \t= " + " " + "\"" + crea_um + "\"");
            listBox1.Items.Add("Creator_Original \t= " + " " + "\"" + crea_uo + "\"");
            listBox1.Items.Add(" ");
            listBox1.Items.Add("Description_ENG \t=" + " " + "\"" + d_eng_txt.Text + "\"");
            listBox1.Items.Add("Description_GER \t=" + " " + "\"" + d_ger_txt.Text + "\"");
            listBox1.Items.Add("Description_FRA \t=" + " " + "\"" + d_fra_txt.Text + "\"");
            listBox1.Items.Add("Description_POL \t=" + " " + "\"" + d_pol_txt.Text + "\"");
            listBox1.Items.Add(" ");
            if (checkBox1.Checked == true)
            {
                listBox1.Items.Add("PDFDoc_ENG \t=" + " " + "\"" + pdfgertxt.Text + "\"");
                listBox1.Items.Add("PDFDoc_GER \t=" + " " + "\"" + pdfgertxt.Text + "\"");
                listBox1.Items.Add("PDFDoc_FRA \t=" + " " + "\"" + pdfgertxt.Text + "\"");
                listBox1.Items.Add("PDFDoc_POL \t=" + " " + "\"" + pdfgertxt.Text + "\"");
            }
            else
            {
                listBox1.Items.Add("PDFDoc_ENG \t=" + " " + "\"" + pdfengtxt.Text + "\"");
                listBox1.Items.Add("PDFDoc_GER \t=" + " " + "\"" + pdfgertxt.Text + "\"");
                listBox1.Items.Add("PDFDoc_FRA \t=" + " " + "\"" + pdffratxt.Text + "\"");
                listBox1.Items.Add("PDFDoc_POL \t=" + " " + "\"" + pdfpoltxt.Text + "\"");
            }

            listBox1.Items.Add(" ");
            {
                int count = dataMA.Rows.Count;
                for (int index = 0; index < count; ++index)
                {
                    string as1 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_eng;
                    string as2 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_ger;
                    string as3 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_fra;
                    string as4 = axis + dataMA.Rows[index].Cells[0].Value.ToString() + str_pol;


                    listBox1.Items.Add(as1 + "\t = \"" + dataMA.Rows[index].Cells[1].Value.ToString() + "\"");
                    listBox1.Items.Add(as2 + "\t = \"" + dataMA.Rows[index].Cells[2].Value.ToString() + "\"");
                    listBox1.Items.Add(as3 + "\t = \"" + dataMA.Rows[index].Cells[3].Value.ToString() + "\"");
                    listBox1.Items.Add(as4 + "\t = \"" + dataMA.Rows[index].Cells[4].Value.ToString() + "\"");

                }
                listBox1.Items.Add(" ");
                if (dataSF.Rows.Count == 0)
                {

                }
                else
                {
                    listBox1.Items.Add("[Model_SignalFunc]");
                }

                int count2 = dataSF.Rows.Count;
                for (int index = 0; index < count2; ++index)
                {
                    string as1 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_eng;
                    string as2 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_ger;
                    string as3 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_fra;
                    string as4 = func1 + dataSF.Rows[index].Cells[0].Value.ToString() + func2 + str_pol;


                    listBox1.Items.Add(as1 + "\t = \"" + dataSF.Rows[index].Cells[1].Value.ToString() + "\"");
                    listBox1.Items.Add(as2 + "\t = \"" + dataSF.Rows[index].Cells[2].Value.ToString() + "\"");
                    listBox1.Items.Add(as3 + "\t = \"" + dataSF.Rows[index].Cells[3].Value.ToString() + "\"");
                    listBox1.Items.Add(as4 + "\t = \"" + dataSF.Rows[index].Cells[4].Value.ToString() + "\"");

                }
            }
            Cursor.Current = Cursors.Default;
        }

        private async void btn_man_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(async () =>
            {
                if (SettingsJB == null) throw new InvalidOperationException(Ui.Text("msg3"));
                CaptureTranslationState();
                dataMA.Rows.Clear();
                dataSF.Rows.Clear();
                if (radioButton4.Checked) { await gertrans_m(); await gertrans_s(); }
                else if (radioButton5.Checked) { await fratrans_m(); await fratrans_s(); }
                else { await engtrans_m(); await engtrans_s(); }
                if (dataMA.Rows.Count == 0 && dataSF.Rows.Count == 0)
                    throw new InvalidOperationException(Ui.Text("msg6"));
            });
        }

        private async Task engtrans_m()
        {
            for (int i = 1; i < 100; i++)
            {
                string def = i.ToString();
                string a1 = SettingsJB["FileInfo"]["MovAxis" + i + "_ENG"].Replace("\"", "").Replace("_", " ").Replace("-", " ");
                if (string.IsNullOrEmpty(a1))
                {

                }
                else
                {
                    string translation1 = await TranslateTextAsync(a1, "en", "de");
                    string a2 = translation1;
                    string translation2 = await TranslateTextAsync(a1, "en", "fr");
                    string a3 = translation2;
                    string translation3 = await TranslateTextAsync(a1, "en", "pl");
                    string a4 = translation3;
                    dataMA.Rows.Add(def, a1, a2, a3, a4);
                }
            }
        }

        private async Task engtrans_s()
        {
            for (int j = 1; j < 100; j++)
            {
                string abc = j.ToString("00");
                string a1 = SettingsJB["Model_SignalFunc"]["Pos" + abc + "_Fn_Name_ENG"].Replace("\"", "").Replace("_", " ").Replace("-", " ");
                if (string.IsNullOrEmpty(a1))
                {

                }
                else
                {
                    string translation1 = await TranslateTextAsync(a1, "en", "de");
                    string a2 = translation1;
                    string translation2 = await TranslateTextAsync(a1, "en", "fr");
                    string a3 = translation2;
                    string translation3 = await TranslateTextAsync(a1, "en", "pl");
                    string a4 = translation3;
                    dataSF.Rows.Add(abc, a1, a2, a3, a4);
                }
            }
        }

        private async Task gertrans_m()
        {
            for (int i = 1; i < 100; i++)
            {
                string def = i.ToString();
                string a2 = SettingsJB["FileInfo"]["MovAxis" + i + "_GER"].Replace("\"", "").Replace("_", " ").Replace("-", " ");
                if (string.IsNullOrEmpty(a2))
                {

                }
                else
                {
                    string translation1 = await TranslateTextAsync(a2, "de", "en");
                    string a1 = translation1;
                    string translation2 = await TranslateTextAsync(a2, "de", "fr");
                    string a3 = translation2;
                    string translation3 = await TranslateTextAsync(a2, "de", "pl");
                    string a4 = translation3;
                    dataMA.Rows.Add(def, a1, a2, a3, a4);
                }
            }
        }

        private async Task gertrans_s()
        {
            for (int j = 1; j < 100; j++)
            {
                string abc = j.ToString("00");
                string a2 = SettingsJB["Model_SignalFunc"]["Pos" + abc + "_Fn_Name_GER"].Replace("\"", "").Replace("_", " ").Replace("-", " ");
                if (string.IsNullOrEmpty(a2))
                {

                }
                else
                {
                    string translation1 = await TranslateTextAsync(a2, "de", "en");
                    string a1 = translation1;
                    string translation2 = await TranslateTextAsync(a2, "de", "fr");
                    string a3 = translation2;
                    string translation3 = await TranslateTextAsync(a2, "de", "pl");
                    string a4 = translation3;
                    dataSF.Rows.Add(abc, a1, a2, a3, a4);
                }
            }
        }

        private async Task fratrans_m()
        {
            for (int i = 1; i < 100; i++)
            {
                string def = i.ToString();
                string a3 = SettingsJB["FileInfo"]["MovAxis" + i + "_FRA"].Replace("\"", "").Replace("_", " ").Replace("-", " ");
                if (string.IsNullOrEmpty(a3))
                {

                }
                else
                {
                    string translation1 = await TranslateTextAsync(a3, "fr", "en");
                    string a1 = translation1;
                    string translation2 = await TranslateTextAsync(a3, "fr", "de");
                    string a2 = translation2;
                    string translation3 = await TranslateTextAsync(a3, "fr", "pl");
                    string a4 = translation3;
                    dataMA.Rows.Add(def, a1, a2, a3, a4);
                }
            }
        }

        private async Task fratrans_s()
        {
            for (int j = 1; j < 100; j++)
            {
                string abc = j.ToString("00");
                string a3 = SettingsJB["Model_SignalFunc"]["Pos" + abc + "_Fn_Name_FRA"].Replace("\"", "").Replace("_", " ").Replace("-", " ");
                if (string.IsNullOrEmpty(a3))
                {

                }
                else
                {
                    string translation1 = await TranslateTextAsync(a3, "fr", "en");
                    string a1 = translation1;
                    string translation2 = await TranslateTextAsync(a3, "fr", "de");
                    string a2 = translation2;
                    string translation3 = await TranslateTextAsync(a3, "fr", "pl");
                    string a4 = translation3;
                    dataSF.Rows.Add(abc, a1, a2, a3, a4);
                }
            }
        }

        private async Task engtrans_n()
        {
            await TranslateModelTextsAsync("en");
        }

        private async Task gertrans_n()
        {
            await TranslateModelTextsAsync("de");
        }

        private async Task fratrans_n()
        {
            await TranslateModelTextsAsync("fr");
        }

        private void about_Click(object sender, EventArgs e)
        {
            about ab = new about();
            ab.ShowDialog();
        }



        private void info_Click(object sender, EventArgs e)
        {
            if (File.Exists(infoscreen.InformationPath))
            {
                infoscreen ins = new infoscreen();
                ins.ShowDialog();
            }
            else
            {
                MessageBox.Show(Ui.Text("missingInfo"), "Info");
            }
        }


        private void FolderOpen_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                Properties.Settings.Default.pathdir = folderBrowserDialog1.SelectedPath;
                dlgOpen.InitialDirectory = dlgOpen_eep.InitialDirectory = folderBrowserDialog1.SelectedPath;
                Properties.Settings.Default.Save();
                directoryTreeViewOnDemand1.BeginInit();
                directoryTreeViewOnDemand1.EndInit();
                directoryTreeViewOnDemand1.Visible = true;
                label23.Visible = true;
                label24.Visible = false;
            }
        }

        private async void DirectoryTreeViewOnDemand1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            await OpenSelectedTreeNodeAsync(e.Node);
        }

        internal async Task OpenSelectedTreeNodeAsync(TreeNode node, Action<string> notify = null)
        {
            string path = node?.FullPath;
            if (string.IsNullOrWhiteSpace(path) || Directory.Exists(path) || !File.Exists(path) ||
                !string.Equals(Path.GetExtension(path), ".ini", StringComparison.OrdinalIgnoreCase))
            {
                string message = Ui.Text("chooseIniFile");
                if (notify != null) notify(message);
                else MessageBox.Show(this, message, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            await RunTranslationOperationAsync(async () =>
            {
                btn_def_trl.Enabled = false;
                preview.Enabled = false;
                dataMA.Rows.Clear();
                dataMA.Refresh();
                dataSF.Rows.Clear();
                dataSF.Refresh();
                listBox1.Items.Clear();
                richTextBox1.Clear();
                label21.Text = "";

                // Use the selected node, including keyboard navigation, not the mouse position.
                Properties.Settings.Default.pathfile = path;
                bodyname = GetEncodingBodyName(path);
                label21.Text = " " + path;
                richTextBox1.LoadFile(path, RichTextBoxStreamType.PlainText);
                datpath = path;
                await inilesen(path);
            });
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!Properties.Settings.Default.rememberLastFolder) Properties.Settings.Default.pathdir = null;
            if (updateShutdown)
            {
                Properties.Settings.Default.Save();
                return;
            }
            Properties.Settings.Default.pf_i = null;
            Properties.Settings.Default.pf_ia = null;
            Properties.Settings.Default.pf_m = null;
            Properties.Settings.Default.pf_ma = null;
            Properties.Settings.Default.Save();
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + "iet\\version.txt"))
            {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + "iet\\version.txt");
            }
            if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + "iet\\info.dat"))
            {
                File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\" + "iet\\info.dat");
            }
            Properties.Settings.Default.complet = false;
        }

        private void RadioButton1_CheckedChanged_1(object sender, EventArgs e)
        {
            Properties.Settings.Default.read_only = true;
        }

        private void RadioButton2_CheckedChanged_1(object sender, EventArgs e)
        {
            Properties.Settings.Default.read_only = false;
        }

        private void Button12_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.exp == true)
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = Ui.Text("tableFilter")
                };
                string result = Path.GetFileNameWithoutExtension(Properties.Settings.Default.pathfile);
                sfd.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.pathfile); ;
                sfd.FileName = result + ".csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ToCsV(dataMA, sfd.FileName);
                }
            }
            else
            {
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = Ui.Text("tableFilter")
                };
                string result = Path.GetFileNameWithoutExtension(Properties.Settings.Default.pathfile);
                sfd.InitialDirectory = Path.GetDirectoryName(Properties.Settings.Default.pathfile); ;
                sfd.FileName = result + ".csv";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ToCsV(dataSF, sfd.FileName);
                }
            }

        }

        private void copyAlltoClipboardMA()
        {
            dataMA.SelectAll();
            DataObject dataObj = dataMA.GetClipboardContent();
            if (dataObj != null)
            {
                Clipboard.SetDataObject(dataObj);
            }
        }

        private void copyAlltoClipboardSF()
        {
            dataSF.SelectAll();
            DataObject dataObj = dataSF.GetClipboardContent();
            if (dataObj != null)
            {
                Clipboard.SetDataObject(dataObj);
            }
        }

        private void ToCsV(DataGridView dGV, string filename)
        {
            TableExport.Write(dGV, filename);
        }

        private void MoveAxis_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.exp = true;
        }

        private void SignFunc_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.exp = false;
        }

        private async void Button13_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(async () =>
            {
            if (dlgOpen_eep.ShowDialog() == DialogResult.OK)
            {
                zwa = dlgOpen_eep.FileName;
                RememberWorkingDirectory(zwa);
                await inilesen2(zwa);
            }

            });
        }
        private async Task trans2()
        {
            await eep_trans_ger_to_0();
            await eep_trans_eng_to_0();
            await eep_trans_fra_to_0();

            if (numericUpDown1.Maximum == 1)
            {
                await eep_trans_ger_to_1();
                await eep_trans_eng_to_1();
                await eep_trans_fra_to_1();
            }
            if (numericUpDown1.Maximum == 2)
            {
                await eep_trans_ger_to_1();
                await eep_trans_eng_to_1();
                await eep_trans_fra_to_1();

                await eep_trans_ger_to_2();
                await eep_trans_eng_to_2();
                await eep_trans_fra_to_2();
            }
            if (numericUpDown1.Maximum == 3)
            {
                await eep_trans_ger_to_1();
                await eep_trans_eng_to_1();
                await eep_trans_fra_to_1();

                await eep_trans_ger_to_2();
                await eep_trans_eng_to_2();
                await eep_trans_fra_to_2();

                await eep_trans_ger_to_3();
                await eep_trans_eng_to_3();
                await eep_trans_fra_to_3();
            }
            if (numericUpDown1.Maximum == 4)
            {
                await eep_trans_ger_to_1();
                await eep_trans_eng_to_1();
                await eep_trans_fra_to_1();

                await eep_trans_ger_to_2();
                await eep_trans_eng_to_2();
                await eep_trans_fra_to_2();

                await eep_trans_ger_to_3();
                await eep_trans_eng_to_3();
                await eep_trans_fra_to_3();

                await eep_trans_ger_to_4();
                await eep_trans_eng_to_4();
                await eep_trans_fra_to_4();
            }
        }

        private async Task eep_trans_ger_to_0()
        {
            if (Properties.Settings.Default.language == "D")
                await TranslateInstallationSetAsync(0, "de");
        }

        private async Task eep_trans_eng_to_0()
        {
            if (Properties.Settings.Default.language == "E")
                await TranslateInstallationSetAsync(0, "en");
        }

        private async Task eep_trans_fra_to_0()
        {
            if (Properties.Settings.Default.language == "F")
                await TranslateInstallationSetAsync(0, "fr");
        }

        private async Task eep_trans_ger_to_1()
        {
            if (Properties.Settings.Default.language == "D")
                await TranslateInstallationSetAsync(1, "de");
        }


        private async Task eep_trans_eng_to_1()
        {
            if (Properties.Settings.Default.language == "E")
                await TranslateInstallationSetAsync(1, "en");
        }

        private async Task eep_trans_fra_to_1()
        {
            if (Properties.Settings.Default.language == "F")
                await TranslateInstallationSetAsync(1, "fr");
        }

        private async Task eep_trans_ger_to_2()
        {
            if (Properties.Settings.Default.language == "D")
                await TranslateInstallationSetAsync(2, "de");
        }

        private async Task eep_trans_eng_to_2()
        {
            if (Properties.Settings.Default.language == "E")
                await TranslateInstallationSetAsync(2, "en");
        }

        private async Task eep_trans_fra_to_2()
        {
            if (Properties.Settings.Default.language == "F")
                await TranslateInstallationSetAsync(2, "fr");
        }

        private async Task eep_trans_ger_to_3()
        {
            if (Properties.Settings.Default.language == "D")
                await TranslateInstallationSetAsync(3, "de");
        }

        private async Task eep_trans_eng_to_3()
        {
            if (Properties.Settings.Default.language == "E")
                await TranslateInstallationSetAsync(3, "en");
        }

        private async Task eep_trans_fra_to_3()
        {
            if (Properties.Settings.Default.language == "F")
                await TranslateInstallationSetAsync(3, "fr");
        }

        private async Task eep_trans_ger_to_4()
        {
            if (Properties.Settings.Default.language == "D")
                await TranslateInstallationSetAsync(4, "de");
        }

        private async Task eep_trans_eng_to_4()
        {
            if (Properties.Settings.Default.language == "E")
                await TranslateInstallationSetAsync(4, "en");
        }

        private async Task eep_trans_fra_to_4()
        {
            if (Properties.Settings.Default.language == "F")
                await TranslateInstallationSetAsync(4, "fr");
        }

        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            beta21 = numericUpDown1.Value.ToString();
            if (beta21 == "0")
            {
                panel1.Visible = true;
                panel2.Visible = false;
                panel3.Visible = false;
                panel4.Visible = false;
                panel5.Visible = false;
            }
            else if (beta21 == "1")
            {
                panel1.Visible = false;
                panel2.Visible = true;
                panel3.Visible = false;
                panel4.Visible = false;
                panel5.Visible = false;
            }
            else if (beta21 == "2")
            {
                panel1.Visible = false;
                panel2.Visible = false;
                panel3.Visible = true;
                panel4.Visible = false;
                panel5.Visible = false;
            }
            else if (beta21 == "3")
            {
                panel1.Visible = false;
                panel2.Visible = false;
                panel3.Visible = false;
                panel4.Visible = true;
                panel5.Visible = false;
            }
            else if (beta21 == "4")
            {
                panel1.Visible = false;
                panel2.Visible = false;
                panel3.Visible = false;
                panel4.Visible = false;
                panel5.Visible = true;
            }
        }

        private async void btn_trl_1_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(() => TranslateInstallationSetAsync(0, culture_set));
        }
        private async void btn_trl_2_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(() => TranslateInstallationSetAsync(1, culture_set));
        }
        private async void btn_trl_3_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(() => TranslateInstallationSetAsync(2, culture_set));
        }
        private async void btn_trl_5_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(() => TranslateInstallationSetAsync(4, culture_set));
        }
        private async void btn_trl_4_Click(object sender, EventArgs e)
        {
            await RunTranslationOperationAsync(() => TranslateInstallationSetAsync(3, culture_set));
        }
        private void TabControl1_TabIndexChanged(object sender, EventArgs e)
        {
            if (tabPage5 == tabControl1.SelectedTab)
            {
                panel1.Visible = true;
                panel2.Visible = false;
                panel3.Visible = false;
                panel4.Visible = false;
                panel5.Visible = false;
            }
        }

        private void btn_save_Click(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.par_i == true)
            {
                test555 = directoryPath = Path.GetDirectoryName(Properties.Settings.Default.pf_i);
                dlgSave_eep.FileName = Path.GetFileName(Properties.Settings.Default.pf_i);
                dlgSave_eep.InitialDirectory = directoryPath;
                if (dlgSave_eep.ShowDialog() == DialogResult.OK)
                {
                    inischreiben(dlgSave_eep.FileName);
                }
            }
            else if (Properties.Settings.Default.par_i == false)
            {
                directoryPath = Path.GetDirectoryName(zwa);

                dlgSave_eep.InitialDirectory = directoryPath;
                dlgSave_eep.FileName = Path.GetFileName(zwa);
                if (dlgSave_eep.ShowDialog() == DialogResult.OK)
                {
                    inischreiben(dlgSave_eep.FileName);
                }
            }
        }




        private void Translation_Click(object sender, EventArgs e)
        {
            string helper = Path.Combine(Application.StartupPath, "help", "Info_trans.exe");
            if (!File.Exists(helper))
            {
                MessageBox.Show(Properties.Resources.zusatz);
                return;
            }
            Process.Start(new ProcessStartInfo(helper)
            {
                WorkingDirectory = Path.GetDirectoryName(helper),
                Arguments = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
            });
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            option set = new option();
            set.ShowDialog();
        }



        private void TextB_icon_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textB_icon.Text, out int xyz)) icongrafik(xyz);
        }















































        private void label24_Click(object sender, EventArgs e)
        {

        }
    }
}

