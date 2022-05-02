using System;
using System.IO;
using System.Runtime.InteropServices;
using JetBrains.Diagnostics;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
    public static class Kernel32Dll
    {
        private static class UnsafeNativeMethods
        {
            [DllImport("kernel32", SetLastError=true, CharSet = CharSet.Ansi)]
            public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);
        
            [DllImport("kernel32", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        }
        
        private static IntPtr ourNativeLibraryHandle;
        
        private static TType LoadFunction<TType>(string name) where TType: class
        {
            if (ourNativeLibraryHandle == IntPtr.Zero)
                throw new Exception("native library was not loaded");
        
            var addr = UnsafeNativeMethods.GetProcAddress(ourNativeLibraryHandle, name);
            return Marshal.GetDelegateForFunctionPointer(addr, typeof(TType)) as TType;
        }
        
        private delegate void StartProfilingDelegate();

        public static void StartProfiling(string fullPath)
        { 
            if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE) 
                Debug.Log($"StartProfiling: {fullPath}");
            // for Windows we can invoke StartProfiling without coping the assembly
            if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows)
            {
                ourNativeLibraryHandle = UnsafeNativeMethods.LoadLibrary(fullPath);
                var function = LoadFunction<StartProfilingDelegate>("StartProfiling");
            
                function();
            }
            // for other OS, we copy assembly once and after one Reload it would become possible to invoke it
            // so first attempt may fail
            else
            {
                var jbProfiler = new FileInfo(fullPath);
                var targetJbProfiler = new FileInfo($"Library/mono-profiler-jb.dll");
                if (!targetJbProfiler.Exists || jbProfiler.Length != targetJbProfiler.Length)
                {
                    jbProfiler.CopyTo(targetJbProfiler.FullName, true);
                    UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
                }

                StartProfiling();    
            }
        }
        
        // On Rider side we pre-copy dll to Library
        [DllImport("Library/mono-profiler-jb.dll")]
        public static extern void StartProfiling();

    }
}