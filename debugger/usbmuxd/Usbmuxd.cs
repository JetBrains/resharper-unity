using System;
using System.IO;
using System.Runtime.InteropServices;

namespace JetBrains.Debugger.Worker.Plugins.Unity
{
    // Modified from https://github.com/Unity-Technologies/MonoDevelop.Debugger.Soft.Unity/blob/unity-staging/iOSOverUsbSupport.cs
    //
    // Copyright (c) Unity Technologies
    //
    // All rights reserved.
    //
    // MIT License
    //
    // Permission is hereby granted, free of charge, to any person obtaining a copy
    // of this software and associated documentation files (the "Software"), to deal
    // in the Software without restriction, including without limitation the rights
    // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    // copies of the Software, and to permit persons to whom the Software is
    // furnished to do so, subject to the following conditions:
    //
    // The above copyright notice and this permission notice shall be included in all
    // copies or substantial portions of the Software.
    //
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    // SOFTWARE.
    public static class Usbmuxd
    {
        // Note: This struct is used in .Net for interop. so do not change it, or know what you are doing!
        // ReSharper disable InconsistentNaming
        // ReSharper disable FieldCanBeMadeReadOnly.Global
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
        public struct iOSDevice
        {
            public int productId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=41)]
            public string udid;
        }
        // ReSharper restore FieldCanBeMadeReadOnly.Global
        // ReSharper restore InconsistentNaming

        // Folder structure:
        // iOSSupport/ - Intel Mac .dylib
        //   x86/      - 32 bit Windows DLL. Obsolete (from iOSOverUsbSupport.cs)
        //   x86_64/   - 64 bit Windows DLL + Linux .so
        //
        // Cleaned up in 2021.2+
        // iOSSupport/
        //   arm64/    - 64 bit ARM. Only M1 Mac .dylib
        //   x64/      - 64 bit Intel. Windows .dll, Intel Mac .dylib, Linux .so

        private const string NativeDylibOsxX64 = @"x64/UnityEditor.iOS.Native.dylib";
        private const string NativeDylibOsxArm64 = @"arm64/UnityEditor.iOS.Native.dylib";
        private const string NativeDylibOsxFallback = "UnityEditor.iOS.Native.dylib";

        private const string NativeDllWinX64 = @"x64\UnityEditor.iOS.Native.dll";
        private const string NativeDllWinX64Fallback = @"x86_64\UnityEditor.iOS.Native.dll";
        private const string NativeDllWinX86Obsolete = @"x86\UnityEditor.iOS.Native.dll";

        private const string NativeSoLinuxX64 = "x64/UnityEditor.iOS.Native.so";
        private const string NativeSoLinuxX64Fallback = "x86_64/UnityEditor.iOS.Native.so";

        private static readonly IDllLoader ourLoader;
        private static IntPtr ourNativeLibraryHandle;

        public delegate bool StartIosProxyDelegate(ushort localPort, ushort devicePort, [MarshalAs(UnmanagedType.LPStr)] string deviceId);
        public delegate void StopIosProxyDelegate(ushort localPort);
        public delegate void StartUsbmuxdListenThreadDelegate();
        public delegate void StopUsbmuxdListenThreadDelegate();
        public delegate uint UsbmuxdGetDeviceCountDelegate();
        public delegate bool UsbmuxdGetDeviceDelegate(uint index, out iOSDevice device);

        public static StartIosProxyDelegate StartIosProxy;
        public static StopIosProxyDelegate StopIosProxy;
        public static StartUsbmuxdListenThreadDelegate StartUsbmuxdListenThread;
        public static StopUsbmuxdListenThreadDelegate StopUsbmuxdListenThread;
        public static UsbmuxdGetDeviceCountDelegate UsbmuxdGetDeviceCount;
        public static UsbmuxdGetDeviceDelegate UsbmuxdGetDevice;

        public static bool Supported => ourLoader != null;
        public static bool IsDllLoaded => ourNativeLibraryHandle != IntPtr.Zero;

        // Setup correctly, or throw trying
        public static void Setup(string iosSupportPath)
        {
            string libraryPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libraryPath = GetWindowsNativeLibraryPath(iosSupportPath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libraryPath = GetMacOsNativeLibraryPath(iosSupportPath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                libraryPath = GetLinuxNativeLibraryPath(iosSupportPath);
            else
                throw new PlatformNotSupportedException("iOS device enumeration not supported on this platform");

            LoadNativeLibrary(libraryPath);
            InitFunctions();
        }

        public static void Shutdown()
        {
            if (ourNativeLibraryHandle == IntPtr.Zero)
                ourLoader.FreeLibrary(ourNativeLibraryHandle);
        }

        private static string GetWindowsNativeLibraryPath(string iosSupportPath)
        {
            if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                return Path.Combine(iosSupportPath, NativeDllWinX86Obsolete);

            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                throw new PlatformNotSupportedException("No native library for process architecture " +
                                                        RuntimeInformation.ProcessArchitecture);
            }

            var defaultDllPath = Path.Combine(iosSupportPath, NativeDllWinX64);
            if (File.Exists(defaultDllPath))
                return defaultDllPath;

            var fallbackDllPath = Path.Combine(iosSupportPath, NativeDllWinX64Fallback);
            if (File.Exists(fallbackDllPath))
                return fallbackDllPath;

            // Show where we've looked
            Console.WriteLine("Cannot find native library: {0}", defaultDllPath);
            Console.WriteLine("Cannot find native library: {0}", fallbackDllPath);

            // Fall through to default error handling (i.e. throw FileNotFoundException when trying to load the library)
            return defaultDllPath;
        }

        private static string GetMacOsNativeLibraryPath(string iosSupportPath)
        {
            // Native M1 reports as Arm64. Rosetta reports as X64, same as actual Intel X64
            if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            {
                var dylibPath = Path.Combine(iosSupportPath, NativeDylibOsxArm64);
                if (!File.Exists(dylibPath) && Directory.Exists(iosSupportPath))
                {
                    Console.WriteLine("No Apple Silicon native library available at '{0}'", dylibPath);
                    throw new PlatformNotSupportedException(
                        "Apple Silicon support requires a native install of Unity 2021.2 or above");
                }

                return dylibPath;
            }

            var defaultDylibPath = Path.Combine(iosSupportPath, NativeDylibOsxX64);
            if (File.Exists(defaultDylibPath))
                return defaultDylibPath;

            var fallbackDylibPath = Path.Combine(iosSupportPath, NativeDylibOsxFallback);
            if (File.Exists(fallbackDylibPath))
                return fallbackDylibPath;

            // Show where we've looked
            Console.WriteLine("Cannot find native library: {0}", defaultDylibPath);
            Console.WriteLine("Cannot find native library: {0}", fallbackDylibPath);

            // Fall through to default error handling (i.e. throw FileNotFoundException when trying to load the library)
            return defaultDylibPath;
        }

        private static string GetLinuxNativeLibraryPath(string iosSupportPath)
        {
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                throw new PlatformNotSupportedException("No native library for process architecture " +
                                                        RuntimeInformation.ProcessArchitecture);
            }

            var defaultSoPath = Path.Combine(iosSupportPath, NativeSoLinuxX64);
            if (File.Exists(defaultSoPath))
                return defaultSoPath;

            var fallbackSoPath = Path.Combine(iosSupportPath, NativeSoLinuxX64Fallback);
            if (File.Exists(fallbackSoPath))
                return fallbackSoPath;

            // Show where we've looked
            Console.WriteLine("Cannot find native library: {0}", defaultSoPath);
            Console.WriteLine("Cannot find native library: {0}", fallbackSoPath);

            // Fall through to default error handling (i.e. throw FileNotFoundException when trying to load the library)
            return defaultSoPath;
        }

        private static void LoadNativeLibrary(string libraryPath)
        {
            if (ourNativeLibraryHandle == IntPtr.Zero)
            {
                ourNativeLibraryHandle = ourLoader.LoadLibrary(libraryPath);
                if (ourNativeLibraryHandle != IntPtr.Zero)
                    Console.WriteLine("Loaded: " + libraryPath);
                else
                    throw new InvalidOperationException("Couldn't load library: " + libraryPath);
            }
        }

        private static void InitFunctions()
        {
            StartUsbmuxdListenThread = LoadFunction<StartUsbmuxdListenThreadDelegate>("StartUsbmuxdListenThread");
            StopUsbmuxdListenThread = LoadFunction<StopUsbmuxdListenThreadDelegate>("StopUsbmuxdListenThread");
            UsbmuxdGetDeviceCount = LoadFunction<UsbmuxdGetDeviceCountDelegate>("UsbmuxdGetDeviceCount");
            UsbmuxdGetDevice = LoadFunction<UsbmuxdGetDeviceDelegate>("UsbmuxdGetDevice");
            StartIosProxy = LoadFunction<StartIosProxyDelegate>("StartIosProxy");
            StopIosProxy = LoadFunction<StopIosProxyDelegate>("StopIosProxy");
        }

        private static TType LoadFunction<TType>(string name) where TType: class
        {
            if (ourNativeLibraryHandle == IntPtr.Zero)
                throw new Exception("iOS native extension library was not loaded");

            IntPtr addr = ourLoader.GetProcAddress(ourNativeLibraryHandle, name);
            return Marshal.GetDelegateForFunctionPointer(addr, typeof(TType)) as TType;
        }

        static Usbmuxd()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    ourLoader = new PosixDllLoader();
                    break;
                case PlatformID.Win32NT:
                    ourLoader = new WindowsDllLoader();
                    break;
                default:
                    throw new PlatformNotSupportedException("Platform not supported");
            }
        }
    }
}