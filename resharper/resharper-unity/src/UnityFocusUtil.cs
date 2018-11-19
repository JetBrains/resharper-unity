using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class UnityFocusUtil
    {
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int Minimize = 6;
        private const int Restore = 9;
        
        public static void FocusUnity(int pid)
        {
            if (PlatformUtil.RuntimePlatform == PlatformUtil.Platform.Windows)
            {
                var process = Process.GetProcessById(pid);
                var hWnd = process.MainWindowHandle;

                ShowWindow(hWnd, Minimize); // TODO Krasnotsvetov : handle two monitors. We should not minimize unity 
                SetForegroundWindow(hWnd);
                ShowWindow(hWnd, Restore);
            }
            // TODO Krasnotsvetov focus on mac os and linux
        }
    }
}