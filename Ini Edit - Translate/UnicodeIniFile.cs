using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Ini_Edit___Translate
{
    // Same section/key access used by the editor, with explicit Unicode Win32 calls.
    internal sealed class UnicodeIniFile
    {
        private readonly string path;
        internal UnicodeIniFile(string path) { this.path = Path.GetFullPath(path); }
        internal Section this[string name] => new Section(path, name);
        internal sealed class Section
        {
            private readonly string path, section;
            internal Section(string path, string section) { this.path = path; this.section = section; }
            internal string this[string key]
            {
                get
                {
                    var value = new StringBuilder(65536);
                    uint count = GetPrivateProfileString(section, key, "", value, (uint)value.Capacity, path);
                    if (count >= value.Capacity - 1) throw new IOException("INI value exceeds the supported length.");
                    return value.ToString();
                }
                set
                {
                    if (!WritePrivateProfileString(section, key, value, path)) throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint GetPrivateProfileString(string section, string key, string fallback, StringBuilder value, uint size, string path);
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileStringW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(string section, string key, string value, string path);
    }
}
