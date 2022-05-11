using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Diagnostics;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Utils
{
    public static class Kernel32Dll
    {
        // private static class UnsafeNativeMethods
        // {
        //     [DllImport("kernel32", SetLastError=true, CharSet = CharSet.Ansi)]
        //     public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)]string lpFileName);
        //
        //     [DllImport("kernel32", CharSet=CharSet.Ansi, ExactSpelling=true, SetLastError=true)]
        //     public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        // }
        //
        //private static IntPtr ourNativeLibraryHandle;
        //
        // private static TType LoadFunction<TType>(string name) where TType: class
        // {
        //     if (ourNativeLibraryHandle == IntPtr.Zero)
        //         throw new Exception("native library was not loaded");
        //
        //     var addr = UnsafeNativeMethods.GetProcAddress(ourNativeLibraryHandle, name);
        //     return Marshal.GetDelegateForFunctionPointer(addr, typeof(TType)) as TType;
        // }
        //
        // private delegate void StartProfilingDelegate();

        public static void StartProfiling(string dllFile)
        { 
            if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE) 
                Debug.Log($"StartProfiling: {dllFile}");
            // for Windows we can invoke StartProfiling without coping the assembly
            // if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows)
            // {
            //     // val riderPath = Restarter.getIdeStarter()?.toFile()
            //     // if (riderPath == null) throw Error("riderPath is empty.")
            //     // val folderName = when {
            //     //     SystemInfo.isWindows -> "windows-x64/mono-profiler-jb.dll"
            //     //     SystemInfo.isMac -> "macos-x64/libmono-profiler-jb.dylib"
            //     //     SystemInfo.isUnix -> "linux-x64/libmono-profiler-jb.so"
            //     //     else -> throw Error("Unknown OS.")
            //     // }
            //     // val relPath = "plugins/dotCommon/DotFiles/$folderName"
            //     // var profilerDllPath = riderPath.parentFile.parentFile.resolve(relPath)
            //     // // for linux/mac: linux-x64/macos-x64"
            //     // if (!profilerDllPath.exists()) // for locally compiled rider
            //     //     profilerDllPath = File(PathManager.getBinPath()).parentFile.parentFile.parentFile.resolve("Bin.RiderBackend/plugins/dotTrace/DotFiles/windows-x64/mono-profiler-jb.dll")
            //     // if (!profilerDllPath.exists())
            //     //     throw Error("${profilerDllPath.name} was not found.")
            //     
            //     var profilerAssembly = Path.Combine(dllFile, "../windows-x64/mono-profiler-jb.dll");
            //     ourNativeLibraryHandle = UnsafeNativeMethods.LoadLibrary(profilerAssembly);
            //     var function = LoadFunction<StartProfilingDelegate>("StartProfiling");
            //
            //     function();
            // }
            // // for other os, load managed assembly JetBrains.Etw.UnityProfilerApi and use it.
            // else
            // {
                // C:\Work\dotnet-products\Bin.RiderBackend\JetBrains.Etw.UnityProfilerApi.dll
                var assembly = AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(dllFile));

                var type = assembly.GetType("JetBrains.Etw.Api.UnityProfilerApi");
                if (type == null)
                    throw new ApplicationException("Unable to get the type");

                var method = type.GetMethod("StartProfiling");
                if (method == null)
                    throw new ApplicationException("Unable to get the method");

                method.Invoke(null, null); // call StartProfiling
            // }
        }
    }
}