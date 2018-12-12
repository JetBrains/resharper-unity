using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Interop.WinApi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class UnityFocusUtil
    {

        [DllImport("User32.dll")]
        private static extern bool SwitchToThisWindow (IntPtr hwnd, bool fUnknown);
        
        public static void FocusUnity(int pid)
        {
            if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
            {
                var process = Process.GetProcessById(pid);
                var hWnd = process.MainWindowHandle;

                SwitchToThisWindow(hWnd, true);
            }
        }
    }
}