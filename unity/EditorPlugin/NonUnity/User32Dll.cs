using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace JetBrains.Rider.Unity.Editor.NonUnity
{
  public static class User32Dll
  {
    /// <summary>
    /// Gets the ID of the process that owns the window.
    /// Note that creating a <see cref="Process"/> wrapper for that is very expensive because it causes an enumeration of all the system processes to happen.
    /// </summary>
    public static int GetWindowProcessId(IntPtr hwnd)
    {
      uint dwProcessId;
      GetWindowThreadProcessId(hwnd, out dwProcessId);
      return unchecked((int) dwProcessId);
    }

    /// <summary>
    /// Lists the handles of all the top-level windows currently available in the system.
    /// </summary>
    public static List<IntPtr> GetTopLevelWindowHandles()
    {
      var retval = new List<IntPtr>();
      EnumWindowsProc callback = (hwnd, param) =>
      {
        retval.Add(hwnd);
        return 1;
      };
      EnumWindows(Marshal.GetFunctionPointerForDelegate(callback), IntPtr.Zero);
      GC.KeepAlive(callback);
      return retval;
    }

    public delegate int EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true,
      ExactSpelling = true)]
    public static extern int EnumWindows(IntPtr lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true,
      ExactSpelling = true)]
    public static extern int SetForegroundWindow(IntPtr hWnd);
    
    [DllImport("user32.dll")]
    public static extern bool AllowSetForegroundWindow(int dwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true,
      ExactSpelling = true)]
    public static extern uint ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("User32.dll")]
    public static extern bool IsIconic(IntPtr handle);

  }
}