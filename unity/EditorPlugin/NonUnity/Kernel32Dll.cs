using System;
using System.Runtime.InteropServices;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Utils
{
    public class Kernel32Dll
    {
        internal static class UnsafeNativeMethods
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
            ourNativeLibraryHandle = UnsafeNativeMethods.LoadLibrary(fullPath);
            var function = LoadFunction<StartProfilingDelegate>("StartProfiling");

            function();
        }
        
        // for dotnet-products
        // C:/Work/dotnet-products/Plugins/ReSharperUnity/unity/build/EditorPluginNet46/bin/Debug/net472/JetBrains.Rider.Unity.Editor.Plugin.Net46.dll
        // C:/Work/dotnet-products/Bin.RiderBackend/plugins/dotTrace/DotFiles/windows-x64/mono-profiler-jb.dll
        // [DllImport("../../../../../../../../Bin.RiderBackend/plugins/dotTrace/DotFiles/windows-x64/mono-profiler-jb.dll")]
        // for real product
        //[DllImport("../../../../../../../../Bin.RiderBackend/plugins/dotTrace/DotFiles/windows-x64/mono-profiler-jb.dll")]
        [DllImport("mono-profiler-jb.dll")]
        public static extern void StartProfiling();

    }
}