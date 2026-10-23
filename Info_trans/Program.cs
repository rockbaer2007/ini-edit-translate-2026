using System;
using System.IO;
using System.Windows.Forms;

namespace Info_trans
{
    internal static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length == 1 && (args[0] == "de" || args[0] == "en" || args[0] == "fr"))
            {
                var culture = System.Globalization.CultureInfo.GetCultureInfo(args[0]);
                System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
                System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

            System.Threading.Mutex mutex = new System.Threading.Mutex(true, Application.ProductName, out bool createdNew);
            if (createdNew)
            {
                // bitte Form1 ersetzen 
                Application.Run(new Form1());
                // und auch wieder Freigeben besser ist besser
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show(DataFiles.Text("Info_trans läuft bereits.", "Info_trans is already running.",
                    "Info_trans est déjà en cours d’exécution."), "Info_trans", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }
    }
}
