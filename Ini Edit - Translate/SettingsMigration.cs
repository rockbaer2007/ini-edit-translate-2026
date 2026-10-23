using System.Configuration;
using System.IO;

namespace Ini_Edit___Translate
{
    internal static class SettingsMigration
    {
        internal static void UpgradeIfNeeded()
        {
            string configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            if (File.Exists(configuration)) return;
            Properties.Settings.Default.Upgrade();
            Properties.Settings.Default.Save();
        }
    }
}
