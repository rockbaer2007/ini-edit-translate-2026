using System;
using System.Globalization;

namespace Ini_Edit___Translate
{
    internal static class Ui
    {
        internal static string Text(string key)
        {
            return Properties.Resources.ResourceManager.GetString(key, CultureInfo.CurrentUICulture)
                ?? throw new InvalidOperationException("Missing interface resource: " + key);
        }

        internal static string Format(string key, params object[] values)
        {
            return string.Format(CultureInfo.CurrentUICulture, Text(key), values);
        }
    }
}
