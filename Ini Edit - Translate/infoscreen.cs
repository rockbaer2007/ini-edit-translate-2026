using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    public partial class infoscreen : Form
    {
        public infoscreen()
        {
            InitializeComponent();
            richTextBox1.SelectAll();
            richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
            richTextBox1.DeselectAll();
            richTextBox1.LoadFile(Application.StartupPath + "\\" + "info.rtf");
        }
    }
}
