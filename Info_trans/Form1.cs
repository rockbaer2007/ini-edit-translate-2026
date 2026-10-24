using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Info_trans
{
    public partial class Form1 : Form
    {
        private readonly List<string> unavailable = new List<string>();
        private readonly ToolStripStatusLabel dataStatus = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        private string ScriptFileName;
        private string connectionString;

        public Form1()
        {
            InitializeComponent();
            Text = DataFiles.Text("Info_trans – Nachschlagehilfe", "Info_trans – Reference help", "Info_trans – Aide de référence");
            label1.Text = DataFiles.Text("Standardbegriffe und Wörterbücher\r\nAuf einen Eintrag klicken, um ihn in die Zwischenablage zu kopieren.",
                "Standard terms and dictionaries\r\nClick an entry to copy it to the clipboard.",
                "Termes standard et dictionnaires\r\nCliquer sur une entrée pour la copier dans le presse-papiers.");
            var tabNames = new[]
            {
                DataFiles.Text("Immobilien", "Buildings", "Bâtiments"),
                DataFiles.Text("Signale", "Signals", "Signaux"),
                DataFiles.Text("Rollmaterial", "Rolling stock", "Matériel roulant"),
                DataFiles.Text("Gleisobjekte", "Track objects", "Objets de voie"),
                DataFiles.Text("Landschaft", "Landscape", "Paysage"),
                DataFiles.Text("Güter", "Goods", "Marchandises"),
                DataFiles.Text("Datenbankimport", "Database import", "Import de base"),
                DataFiles.Text("Iconhilfe", "Icon reference", "Référence des icônes"),
                DataFiles.Text("Technischer Wortschatz", "Technical vocabulary", "Vocabulaire technique"),
                DataFiles.Text("Wörterbuch DE/EN/FR", "DE/EN/FR dictionary", "Dictionnaire DE/EN/FR"),
                DataFiles.Text("Bahnabkürzung", "Railway abbreviations", "Abréviations ferroviaires")
            };
            tabControl1.Multiline = true;
            for (int index = 0; index < tabNames.Length; index++) tabControl1.TabPages[index].Text = tabNames[index];
            foreach (TabPage page in tabControl1.TabPages)
                foreach (Control control in page.Controls)
                    if (control is DataGridView grid) grid.AllowUserToAddRows = false;
            var statusBar = new StatusStrip();
            statusBar.Items.Add(dataStatus);
            Controls.Add(statusBar);
            label1.Dock = DockStyle.Top;
            tabControl1.MinimumSize = Size.Empty;
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.BringToFront();
            dataGridView1.ReadOnly = true;
            dataGridView2.ReadOnly = true;
            dataGridView3.ReadOnly = true;
            dataGridView4.ReadOnly = true;
            dataGridView5.ReadOnly = true;
            dataGridView6.ReadOnly = true;
            dataGridView7.ReadOnly = true;
            dataGridView8.ReadOnly = true;
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

        private void TabControl1_DrawItem_1(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControl1.TabPages[e.Index];
            e.Graphics.FillRectangle(new SolidBrush(page.BackColor), e.Bounds);

            Rectangle paddedBounds = e.Bounds;
            int yOffset = (e.State == DrawItemState.Selected) ? -2 : 1;
            paddedBounds.Offset(1, yOffset);
            TextRenderer.DrawText(e.Graphics, page.Text, Font, paddedBounds, page.ForeColor);
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControl1.TabPages[e.Index];
            using (var background = new SolidBrush(Color.Lime))
                e.Graphics.FillRectangle(background, e.Bounds);

            Rectangle paddedBounds = e.Bounds;
            int yOffset = (e.State == DrawItemState.Selected) ? -2 : 1;
            paddedBounds.Offset(1, yOffset);
            TextRenderer.DrawText(e.Graphics, page.Text, Font, paddedBounds, Color.Black);
        }

        private static IEnumerable<string> ReadAsLines(string filename)
        {
            using (StreamReader reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    yield return reader.ReadLine();
                }
            }
        }


        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }

        private void DataGridView2_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }

        private void DataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }

        private void DataGridView4_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }

        private void DataGridView5_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }

        private void DataGridView6_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }

        //private void DataGridView7_CellContentClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (dataGridView1.SelectedCells.Count > 0)
        //    {
        //        string clip = dataGridView7.SelectedCells[0].Value.ToString();
        //        Clipboard.SetText(clip);
        //    }
        //}

        private void DataGridView8_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }
        private void DataGridView9_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }

        private void DataGridView10_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            CopySelection(sender);
        }
        private static void CopySelection(object sender)
        {
            var grid = sender as DataGridView;
            if (grid == null || grid.SelectedCells.Count == 0) return;
            string value = Convert.ToString(grid.SelectedCells[0].Value);
            if (!string.IsNullOrEmpty(value)) Clipboard.SetText(value);
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            richTextBox1.SelectAll();
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
            richTextBox1.DeselectAll();
            Center_Text();
            if (File.Exists(DataFiles.PathFor("IET2.FDB")))
                TryLoad("IET2.FDB / Firebird", readdb);
            else
            {
                var names = new[] { "estate", "signals", "stock", "object", "landscape", "goods" };
                var grids = new[] { dataGridView1, dataGridView2, dataGridView3, dataGridView4, dataGridView5, dataGridView6 };
                for (int index = 0; index < names.Length; index++)
                {
                    int current = index;
                    TryLoad(names[index] + ".csv", () => grids[current].DataSource = DataFiles.ReadTable(names[current] + ".csv"));
                }
                button2.Enabled = false;
                toolTip1.SetToolTip(button2, DataFiles.Text("SQL-Import benötigt IET2.FDB und die Firebird-Laufzeit.",
                    "SQL import requires IET2.FDB and the Firebird runtime.",
                    "L’import SQL nécessite IET2.FDB et le moteur Firebird."));
                dataGridView7.DataSource = new DataTable();
                unavailable.Add(DataFiles.Text("IET2.FDB (CSV-Nachschlagetabellen aktiv)",
                    "IET2.FDB (CSV reference tables active)", "IET2.FDB (tables CSV actives)"));
            }
            TryLoad("iconhelp.dat", () => dataGridView8.DataSource = DataFiles.ReadTable("iconhelp.dat"));
            TryLoad("twb.dat", () => dataGridView9.DataSource = DataFiles.ReadTable("twb.dat"));
            TryLoad("Wörterbuch DE/EN/FR", () =>
            {
                var dictionary = DataFiles.ReadDictionary();
                dataGridView10.DataSource = dictionary;
                dataGridView10.ForeColor = Color.Black;
                dataGridView10.DefaultCellStyle.ForeColor = Color.Black;
                dataGridView10.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dataGridView10.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dataGridView10.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
                if (dictionary.Columns.Count == 2)
                    tabPage10.Text = DataFiles.Text("Wörterbuch DE/EN", "DE/EN dictionary", "Dictionnaire DE/EN");
            });
            TryLoad("railway-abbreviations.csv", () =>
            {
                dataGridView11.DataSource = DataFiles.ReadTable("railway-abbreviations.csv");
                foreach (DataGridViewColumn column in dataGridView11.Columns) column.MinimumWidth = 150;
            });
            dataStatus.Text = unavailable.Count == 0
                ? DataFiles.Text("Nachschlagedaten geladen.", "Reference data loaded.", "Données de référence chargées.")
                : DataFiles.Text("Fehlt oder nicht lesbar: ", "Missing or unreadable: ", "Absent ou illisible : ") + string.Join(", ", unavailable);
            dataStatus.ToolTipText = dataStatus.Text;
            /*iconlist()*/
            ;
        }

        private void TryLoad(string name, Action load)
        {
            try { load(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException
                || error is FbException || error is DllNotFoundException || error is BadImageFormatException
                || error is System.Data.DataException || error is ArgumentException
                || error is Microsoft.VisualBasic.FileIO.MalformedLineException
                || error is TypeInitializationException || error is EntryPointNotFoundException
                || error is InvalidOperationException || error is System.ComponentModel.Win32Exception)
            {
                unavailable.Add(name);
            }
        }

        private void iconhelp()
        {
            DataTable dt = new DataTable();
            using (StreamReader sr = new StreamReader("iconhelp.dat"))
            {
                while (!sr.EndOfStream)
                {
                    string[] currentline = sr.ReadLine().Split(new string[] { ";" }, StringSplitOptions.None);
                    if (dt.Columns.Count == 0)
                    {
                        for (int i = 0; i < currentline.Length; i++)
                        {
                            dt.Columns.Add(currentline[i]);
                        }
                    }
                    else
                    {
                        dt.Rows.Add(currentline);
                    }
                }
                sr.Close();
            }
            dataGridView8.DataSource = dt;
        }
        private void twb()
        {
            DataTable dt = new DataTable();
            using (StreamReader sr = new StreamReader("twb.dat"))
            {
                while (!sr.EndOfStream)
                {
                    string[] currentline = sr.ReadLine().Split(new string[] { ";" }, StringSplitOptions.None);
                    if (dt.Columns.Count == 0)
                    {
                        for (int i = 0; i < currentline.Length; i++)
                        {
                            dt.Columns.Add(currentline[i]);
                        }
                    }
                    else
                    {
                        dt.Rows.Add(currentline);
                    }
                }
                sr.Close();
            }
            dataGridView9.DataSource = dt;
        }

        private void ws_d_e()
        {
            DataTable dt4 = new DataTable();
            using (StreamReader sr = new StreamReader("ws_D_E.dat"))
            {
                while (!sr.EndOfStream)
                {
                    string[] currentline = sr.ReadLine().Split(new string[] { ";" }, StringSplitOptions.None);
                    if (dt4.Columns.Count == 0)
                    {
                        for (int i = 0; i < currentline.Length; i++)
                        {
                            dt4.Columns.Add(currentline[i]);
                        }
                    }
                    else
                    {
                        dt4.Rows.Add(currentline);
                    }
                }
                sr.Close();
            }
            dataGridView10.DataSource = dt4;
        }
        //private void iconlist()
        //{
        //    DataTable dt2 = new DataTable();
        //    using (StreamReader sr = new StreamReader("eep_icon2.dat"))
        //    {
        //        while (!sr.EndOfStream)
        //        {
        //            string[] currentline = sr.ReadLine().Split(new string[] { ";" }, StringSplitOptions.None);
        //            if (dt2.Columns.Count == 0)
        //            {
        //                for (int i = 0; i < currentline.Length; i++)
        //                {
        //                    dt2.Columns.Add(currentline[i]);
        //                }
        //            }
        //            else
        //            {
        //                dt2.Rows.Add(currentline);
        //            }
        //        }
        //        sr.Close();
        //    }

        //    dataGridView9.DataSource = dt2;
        //}

        private void Button2_Click(object sender, EventArgs e)
        {
            openSQL.InitialDirectory = DataFiles.Root;
            if (openSQL.ShowDialog() == DialogResult.OK)
            {
                ScriptFileName = openSQL.FileName;
                if (File.Exists(ScriptFileName))
                {
                    try
                    {
                    // A failed backup stops the import before the database is opened.
                    FbConnection.ClearAllPools();
                    DataFiles.BackupDatabase();
                    FbScript script = new FbScript(File.ReadAllText(ScriptFileName));
                    script.UnknownStatement += Script_UnknownStatement;
                    script.Parse();
                    using (FbConnection cn = new FbConnection(connectionString))
                    {
                    FbBatchExecution fbe = new FbBatchExecution(cn);
                    fbe.AppendSqlStatements(script);
                    fbe.Execute();
                    }
                    readdb();
                    }
                    catch (Exception error) when (error is IOException || error is UnauthorizedAccessException
                        || error is FbException || error is DllNotFoundException || error is BadImageFormatException
                        || error is ArgumentException || error is System.Data.DataException)
                    {
                        MessageBox.Show(DataFiles.Text("Datenbankimport fehlgeschlagen. Eine erstellte Sicherung bleibt erhalten.",
                            "Database import failed. Any created backup is retained.",
                            "Échec de l’import de la base. Toute sauvegarde créée est conservée."));
                    }
                }
            }
        }

        private void readdb()
        {
            if (!File.Exists(DataFiles.PathFor("IET2.FDB"))) throw new FileNotFoundException();
            string nativeClient = DataFiles.RequireNativeClient();
            connectionString = new FbConnectionStringBuilder
            {
                UserID = "SYSDBA", Password = "masterkey", Database = DataFiles.PathFor("IET2.FDB"),
                Dialect = 3, Charset = "UTF8", ServerType = FbServerType.Embedded, Pooling = false,
                ClientLibrary = nativeClient
            }.ToString();
            {
                FbDataAdapter adap1 = new FbDataAdapter("SELECT * FROM ESTATE", connectionString);
                DataTable dt1 = new DataTable();
                adap1.Fill(dt1);
                dataGridView1.DataSource = dt1;

                FbDataAdapter adap2 = new FbDataAdapter("SELECT * FROM SIGNALS", connectionString);
                DataTable dt2 = new DataTable();
                adap2.Fill(dt2);
                dataGridView2.DataSource = dt2;

                FbDataAdapter adap3 = new FbDataAdapter("SELECT * FROM ROLLING_STOCK", connectionString);
                DataTable dt3 = new DataTable();
                adap3.Fill(dt3);
                dataGridView3.DataSource = dt3;

                FbDataAdapter adap4 = new FbDataAdapter("SELECT * FROM TRACK_OBJECT", connectionString);
                DataTable dt4 = new DataTable();
                adap4.Fill(dt4);
                dataGridView4.DataSource = dt4;

                FbDataAdapter adap5 = new FbDataAdapter("SELECT * FROM LANDSCAPE", connectionString);
                DataTable dt5 = new DataTable();
                adap5.Fill(dt5);
                dataGridView5.DataSource = dt5;

                FbDataAdapter adap6 = new FbDataAdapter("SELECT * FROM GOODS", connectionString);
                DataTable dt6 = new DataTable();
                adap6.Fill(dt6);
                dataGridView6.DataSource = dt6;

                FbDataAdapter adap7 = new FbDataAdapter("SELECT * FROM DB_UPDATES", connectionString);
                DataTable dt7 = new DataTable();
                adap7.Fill(dt7);
                dataGridView7.DataSource = dt7;
                button2.Enabled = true;
            }
        }

        private void Script_UnknownStatement(object sender, UnknownStatementEventArgs e)
        {
            e.NewStatementType = SqlStatementType.Update;
            e.Handled = true;
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //if (tabControl1.SelectedIndex == 8 || tabControl1.SelectedIndex == 9)
            //{
            //    MessageBox.Show(@"das Laden der Wörterbucher dauert etwas, bitte etwas Geduld" + Environment.NewLine + Environment.NewLine + "loading the dictionaries takes some time, please be patient" + Environment.NewLine + Environment.NewLine + "le chargement des dictionnaires prend un certain temps, merci d'être patient");
            //}
        }
    }
}
