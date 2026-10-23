using System;
using System.Globalization;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {

            string[] CommandLineArgs = Environment.GetCommandLineArgs();
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.culture_l))
            {
                CultureInfo culture = CultureInfo.InstalledUICulture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            else
            {
                CultureInfo culture;
                try { culture = new CultureInfo(Properties.Settings.Default.culture_l); }
                catch (CultureNotFoundException) { culture = CultureInfo.InstalledUICulture; }
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
            }
            var uiLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (uiLanguage != "de" && uiLanguage != "fr") uiLanguage = "en";
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(uiLanguage);
            if (CommandLineArgs.Length > 1 &&
                ((CommandLineArgs[1] == "-reg" && CommandLineArgs.Length < 4) ||
                 (CommandLineArgs[1] != "-reg" && CommandLineArgs.Length < 3)))
            {
                MessageBox.Show(Ui.Text("invalidArguments"), "IET", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Properties.Settings.Default.par_i = false;
            Properties.Settings.Default.par_ia = false;
            Properties.Settings.Default.par_m = false;
            Properties.Settings.Default.par_ma = false;
            Properties.Settings.Default.pf_i = null;
            Properties.Settings.Default.pf_ia = null;
            Properties.Settings.Default.pf_m = null;
            Properties.Settings.Default.pf_ma = null;
            Properties.Settings.Default.language = null;
            Properties.Settings.Default.Save();

            if (CommandLineArgs.Length > 1)
            {
                string uwe22 = CommandLineArgs[1].ToString();
                if (uwe22 == @"-iD")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_i = true;
                    Properties.Settings.Default.pf_i = uwe23;
                    Properties.Settings.Default.language = "D";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-iDa")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_ia = true;
                    Properties.Settings.Default.pf_ia = uwe23;
                    Properties.Settings.Default.language = "D";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-mD")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_m = true;
                    Properties.Settings.Default.pf_m = uwe23;
                    Properties.Settings.Default.language = "D";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-mDa")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_ma = true;
                    Properties.Settings.Default.pf_ma = uwe23;
                    Properties.Settings.Default.language = "D";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-iF")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_i = true;
                    Properties.Settings.Default.pf_i = uwe23;
                    Properties.Settings.Default.language = "F";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-iFa")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_ia = true;
                    Properties.Settings.Default.pf_ia = uwe23;
                    Properties.Settings.Default.language = "F";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-mF")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_m = true;
                    Properties.Settings.Default.pf_m = uwe23;
                    Properties.Settings.Default.language = "F";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-mFa")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_ma = true;
                    Properties.Settings.Default.pf_ma = uwe23;
                    Properties.Settings.Default.language = "F";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-iE")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_i = true;
                    Properties.Settings.Default.pf_i = uwe23;
                    Properties.Settings.Default.language = "E";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-iEa")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_ia = true;
                    Properties.Settings.Default.pf_ia = uwe23;
                    Properties.Settings.Default.language = "E";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-mE")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_m = true;
                    Properties.Settings.Default.pf_m = uwe23;
                    Properties.Settings.Default.language = "E";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-mEa")
                {
                    string uwe23 = CommandLineArgs[2].ToString();
                    Properties.Settings.Default.par_ma = true;
                    Properties.Settings.Default.pf_ma = uwe23;
                    Properties.Settings.Default.language = "E";
                    Properties.Settings.Default.Save();
                }
                else if (uwe22 == @"-reg")
                {
                    string uwe24 = CommandLineArgs[2].ToString();
                    string uwe25 = CommandLineArgs[3].ToString();
                    Properties.Settings.Default.regstring_n = uwe24;
                    Properties.Settings.Default.regstring_k = uwe25;
                    Properties.Settings.Default.setupreg = true;
                }
            }
            else
            {
                Properties.Settings.Default.par_i = false;
                Properties.Settings.Default.par_ia = false;
                Properties.Settings.Default.par_m = false;
                Properties.Settings.Default.par_ma = false;
                Properties.Settings.Default.pf_i = null;
                Properties.Settings.Default.pf_ia = null;
                Properties.Settings.Default.pf_m = null;
                Properties.Settings.Default.pf_ma = null;
                Properties.Settings.Default.language = null;
                Properties.Settings.Default.Save();
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            System.Threading.Mutex mutex = new System.Threading.Mutex(true, Application.ProductName, out bool createdNew);
            if (createdNew)
            {

                Application.Run(new Form1());
                mutex.ReleaseMutex();
            }
            else
            {
                MessageBox.Show(Ui.Text("alreadyRunning"), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }


        }
    }
}
