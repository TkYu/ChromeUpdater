using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChromeUpdater
{
    public static class Win32Api
    {
        internal const int MF_BYCOMMAND = 0x00000000;
        internal const int SC_CLOSE = 0xF060;

        [Flags]
        public enum LoadLibraryFlags : uint
        {
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, [Out] StringBuilder lParam);

        [DllImport("user32.dll")]
        internal static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        internal static extern IntPtr FindWindowEx(IntPtr hwndParent, uint hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr FindResource(IntPtr hModule, string lpName, string lpType);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern bool FreeResource(IntPtr hglbResource);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr lib);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int LoadString(IntPtr hInstance, uint uID, StringBuilder lpBuffer, int nBufferMax);

        public static string LoadRCString(string path, string lpName, string lpType)
        {
            string ret = null;
            var handle = LoadLibraryEx(path, IntPtr.Zero, LoadLibraryFlags.LOAD_LIBRARY_AS_DATAFILE);
            if (handle != IntPtr.Zero)
            {
                var hRes = FindResource(handle, lpName, lpType);
                if (hRes != IntPtr.Zero)
                {
                    var size = SizeofResource(handle, hRes);
                    var rc = LoadResource(handle, hRes);
                    if (rc != IntPtr.Zero)
                    {
                        var bPtr = new byte[size];
                        Marshal.Copy(rc, bPtr, 0, (int)size);
                        ret = Encoding.Unicode.GetString(bPtr);
                        FreeResource(rc);
                    }
                }
                FreeLibrary(handle);
            }
            return ret;
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWow64Process([In] IntPtr processHandle, [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process);

        public static bool Is64BitProcess = IntPtr.Size == 8;
        public static bool Is64BitOperatingSystem = Is64BitProcess || InternalCheckIsWow64();

        private static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6)
            {
                using (var p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    return IsWow64Process(p.Handle, out retVal) && retVal;
                }
            }
            return false;
        }

        public static class IO
        {
            internal enum FileFuncFlags : uint
            {
                FO_MOVE = 0x1,
                FO_COPY = 0x2,
                FO_DELETE = 0x3,
                FO_RENAME = 0x4
            }
            [Flags]
            internal enum FILEOP_FLAGS : ushort
            {
                FOF_MULTIDESTFILES = 0x1,
                FOF_CONFIRMMOUSE = 0x2,
                /// <summary>
                /// Don't create progress/report
                /// </summary>
                FOF_SILENT = 0x4,
                FOF_RENAMEONCOLLISION = 0x8,
                /// <summary>
                /// Don't prompt the user.
                /// </summary>
                FOF_NOCONFIRMATION = 0x10,
                /// <summary>
                /// Fill in SHFILEOPSTRUCT.hNameMappings.
                /// Must be freed using SHFreeNameMappings
                /// </summary>
                FOF_WANTMAPPINGHANDLE = 0x20,
                FOF_ALLOWUNDO = 0x40,
                /// <summary>
                /// On *.*, do only files
                /// </summary>
                FOF_FILESONLY = 0x80,
                /// <summary>
                /// Don't show names of files
                /// </summary>
                FOF_SIMPLEPROGRESS = 0x100,
                /// <summary>
                /// Don't confirm making any needed dirs
                /// </summary>
                FOF_NOCONFIRMMKDIR = 0x200,
                /// <summary>
                /// Don't put up error UI
                /// </summary>
                FOF_NOERRORUI = 0x400,
                /// <summary>
                /// Dont copy NT file Security Attributes
                /// </summary>
                FOF_NOCOPYSECURITYATTRIBS = 0x800,
                /// <summary>
                /// Don't recurse into directories.
                /// </summary>
                FOF_NORECURSION = 0x1000,
                /// <summary>
                /// Don't operate on connected elements.
                /// </summary>
                FOF_NO_CONNECTED_ELEMENTS = 0x2000,
                /// <summary>
                /// During delete operation, 
                /// warn if nuking instead of recycling (partially overrides FOF_NOCONFIRMATION)
                /// </summary>
                FOF_WANTNUKEWARNING = 0x4000,
                /// <summary>
                /// Treat reparse points as objects, not containers
                /// </summary>
                FOF_NORECURSEREPARSE = 0x8000
            }
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            internal struct SHFILEOPSTRUCT
            {
                public IntPtr hwnd;
                public FileFuncFlags wFunc;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string pFrom;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string pTo;
                public FILEOP_FLAGS fFlags;
                [MarshalAs(UnmanagedType.Bool)]
                public bool fAnyOperationsAborted;
                public IntPtr hNameMappings;
                [MarshalAs(UnmanagedType.LPWStr)]
                public string lpszProgressTitle;
            }
            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            internal static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

            private static string GetErrorString(int n)
            {
                switch (n)
                {
                    case 2: return "Could not find file specific.";
                    case 7: return "Controllers has been destroyed.";
                    case 0x71: return "The source and destination files are the same file.";
                    case 0x72: return "Multiple file paths were specified in the source buffer, but only one destination file path.";
                    case 0x73: return "Rename operation was specified but the destination path is a different directory. Use the move operation instead.";
                    case 0x74: return "The source is a root directory, which cannot be moved or renamed.";
                    case 0x75: return "The operation was canceled by the user, or silently canceled if the appropriate flags were supplied to SHFileOperation.";
                    case 0x76: return "The destination is a subtree of the source.";
                    case 0x78: return "Security settings denied access to the source.";
                    case 0x79: return "The source or destination path exceeded or would exceed MAX_PATH.";
                    case 0x7A: return "The operation involved multiple destination paths, which can fail in the case of a move operation.";
                    case 0x7C: return "The path in the source or destination or both was invalid.";
                    case 0x7D: return "The source and destination have the same parent folder.";
                    case 0x7E: return "The destination path is an existing file.";
                    case 0x80: return "The destination path is an existing folder.";
                    case 0x81: return "The name of the file exceeds MAX_PATH.";
                    case 0x82: return "The destination is a read-only CD-ROM, possibly unformatted.";
                    case 0x83: return "The destination is a read-only DVD, possibly unformatted.";
                    case 0x84: return "The destination is a writable CD-ROM, possibly unformatted.";
                    case 0x85: return "The file involved in the operation is too large for the destination media or file system.";
                    case 0x86: return "The source is a read-only CD-ROM, possibly unformatted.";
                    case 0x87: return "The source is a read-only DVD, possibly unformatted.";
                    case 0x88: return "The source is a writable CD-ROM, possibly unformatted.";
                    case 0xB7: return "MAX_PATH was exceeded during the operation.";
                    case 0x402: return "An unknown error occurred. This is typically due to an invalid path in the source or destination. This error does not occur on Windows Vista and later.";
                    case 0x10000: return "An unspecified error occurred on the destination.";
                    case 0x10074: return "Destination is a root directory and cannot be renamed.";
                    default:
                        return "Unknow：" + n;
                }
            }

            private static int SHFileOperation(string fileName, bool toRecycle, bool showDialog, bool showProgress)
            {
                SHFILEOPSTRUCT lpFileOp = new SHFILEOPSTRUCT
                {
                    wFunc = FileFuncFlags.FO_DELETE,
                    pFrom = fileName + "\0",
                    fFlags = FILEOP_FLAGS.FOF_NOERRORUI
                };

                if (toRecycle)
                    lpFileOp.fFlags |= FILEOP_FLAGS.FOF_ALLOWUNDO;
                if (!showDialog)
                    lpFileOp.fFlags |= FILEOP_FLAGS.FOF_NOCONFIRMATION;
                if (!showProgress)
                    lpFileOp.fFlags |= FILEOP_FLAGS.FOF_SILENT;

                lpFileOp.fAnyOperationsAborted = true;

                return SHFileOperation(ref lpFileOp);
            }

            private static int SHFileOperation(FileFuncFlags flag, string sourceFileName, string destinationFileName, bool showDialog, bool showProgress, bool autoRename)
            {
                SHFILEOPSTRUCT lpFileOp = new SHFILEOPSTRUCT
                {
                    wFunc = flag,
                    pFrom = sourceFileName + "\0",
                    pTo = destinationFileName + "\0\0",
                    fFlags = FILEOP_FLAGS.FOF_NOERRORUI
                };

                lpFileOp.fFlags |= FILEOP_FLAGS.FOF_NOCONFIRMMKDIR;
                if (!showDialog)
                    lpFileOp.fFlags |= FILEOP_FLAGS.FOF_NOCONFIRMATION;
                if (!showProgress)
                    lpFileOp.fFlags |= FILEOP_FLAGS.FOF_SILENT;
                if (autoRename)
                    lpFileOp.fFlags |= FILEOP_FLAGS.FOF_RENAMEONCOLLISION;

                lpFileOp.fAnyOperationsAborted = true;

                return SHFileOperation(ref lpFileOp);
            }

            public static void DeleteFile(string fileName, bool toRecycle = false, bool showDialog = false, bool showProgress = false)
            {
                var ret = SHFileOperation(fileName, toRecycle, showDialog, showProgress);
                if (ret != 0) throw new System.IO.IOException($"Error:{ret}({GetErrorString(ret)})");
            }

            public static void MoveUp(string source, bool showDialog = false, bool showProgress = false, bool autoRename = false)
            {
                var dfName = System.IO.Path.GetDirectoryName(source);
                var ret = SHFileOperation(FileFuncFlags.FO_MOVE, source + "\\*.*", dfName, showDialog, showProgress, autoRename);
                if (ret != 0) throw new System.IO.IOException($"Error:{ret}({GetErrorString(ret)})");
                ret = SHFileOperation(source, false, showDialog, showProgress);
                if (ret != 0) throw new System.IO.IOException($"Error:{ret}({GetErrorString(ret)})");
            }
        }
    }
}
