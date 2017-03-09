using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChromeUpdater
{
    public class Ini
    {
        public string Path { get; }

        public const uint MAX_BUFFER = 32767;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        internal static extern bool WritePrivateProfileSection(string lpAppName, string lpString, string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        internal static extern uint GetPrivateProfileSection(string lpAppName, IntPtr lpReturnedString, uint nSize, string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        internal static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        internal static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public Ini(string iniPath)
        {
            Path = iniPath;
        }

        public string Read(string Key, string Section)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string Section)
        {
            WritePrivateProfileString(Section, Key, Value, Path);
        }
    }
}
