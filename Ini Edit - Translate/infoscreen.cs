using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    public partial class infoscreen : Form
    {
        internal static string InformationPath
        {
            get
            {
                string language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                if (language != "de" && language != "fr") language = "en";
                string localized = System.IO.Path.Combine(Application.StartupPath, "Info." + language + ".rtf");
                return System.IO.File.Exists(localized) ? localized : System.IO.Path.Combine(Application.StartupPath, "Info.rtf");
            }
        }
        public infoscreen()
        {
            InitializeComponent();
            Text = Ui.Text("infoTitle");
            richTextBox1.SelectAll();
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
            richTextBox1.DeselectAll();
            richTextBox1.LoadFile(InformationPath);
        }
    }
}
