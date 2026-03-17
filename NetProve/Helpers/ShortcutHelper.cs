using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace NetProve.Helpers
{
    /// <summary>
    /// Creates a desktop shortcut for the application on first run.
    /// Uses COM IShellLink — no external dependencies.
    /// </summary>
    public static class ShortcutHelper
    {
        // ── COM interfaces for creating .lnk shortcuts ──────────────────────
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink { }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        /// <summary>
        /// Creates a desktop shortcut if one doesn't already exist.
        /// Returns true if shortcut was created, false if it already existed.
        /// </summary>
        public static bool CreateDesktopShortcutIfNeeded()
        {
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var shortcutPath = Path.Combine(desktopPath, "NetProve.lnk");

                // Don't recreate if already exists
                if (File.Exists(shortcutPath)) return false;

                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return false;

                var workingDir = Path.GetDirectoryName(exePath);
                var iconPath = Path.Combine(workingDir ?? "", "app.ico");

                var link = (IShellLink)new ShellLink();
                link.SetPath(exePath);
                link.SetDescription("NetProve — Gaming & Network Performance Optimizer");
                link.SetWorkingDirectory(workingDir ?? "");

                // Use app.ico if available
                if (File.Exists(iconPath))
                    link.SetIconLocation(iconPath, 0);
                else
                    link.SetIconLocation(exePath, 0);

                // Save the shortcut
                var file = (IPersistFile)link;
                file.Save(shortcutPath, false);

                return true;
            }
            catch
            {
                // Non-critical — don't crash the app if shortcut fails
                return false;
            }
        }
    }
}
