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
        private static extern bool SetForegroundWindow(IntPtr handle);
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        private const int Minimize = 6;
        private const int Restore = 9;
        
        public static void FocusUnity(int pid)
        {
            if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
            {
                var process = Process.GetProcessById(pid);
                var hWnd = process.MainWindowHandle;

                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, Restore);
                }
                // ShowWindow(hWnd, Minimize); // TODO Krasnotsvetov : handle two monitors. We should not minimize unity 
                SetForegroundWindow(hWnd);
            }
            // TODO Krasnotsvetov focus on mac os and linux
        }
    }
}