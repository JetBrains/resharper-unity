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
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
        public struct iOSDevice
        {
            public int productId;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=41)]
            public string udid;
        }
        // ReSharper restore InconsistentNaming

        // On my Windows 2019.3.5f1 install, this file is x86_64\UnityEditor.iOS.Native.dll
        // 5.6.7f1 (Mac), the file is UnityEditor.iOS.Native.dylib. Will need to check on Windows
        // 5.6.7f1 (Windows) => UnityEditor.iOS.Native.dll
        // Unity 2021.2 introduced M1 support on Mac and moved the default (x64) native lib from iOSSupport to
        // iOSSupport/x64. Make sure we support the fallback location
        private const string NativeDylibOsxX64 = @"x64/UnityEditor.iOS.Native.dylib";
        private const string NativeDylibOsxArm64 = @"arm64/UnityEditor.iOS.Native.dylib";
        private const string NativeDylibOsxFallback = "UnityEditor.iOS.Native.dylib";
        private const string NativeDllWin32 = @"x86\UnityEditor.iOS.Native.dll";
        private const string NativeDllWin64 = @"x86_64\UnityEditor.iOS.Native.dll";

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

        private static TType LoadFunction<TType>(string name) where TType: class
        {
            if (ourNativeLibraryHandle == IntPtr.Zero)
                throw new Exception("iOS native extension library was not loaded");

            IntPtr addr = ourLoader.GetProcAddress(ourNativeLibraryHandle, name);
            return Marshal.GetDelegateForFunctionPointer(addr, typeof(TType)) as TType;
        }

        // Setup correctly, or throw trying
        public static void Setup(string iosSupportPath)
        {
            // TODO: Does Unity ship a native library for Linux?
            string libraryPath;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                libraryPath = GetWindowsNativeLibraryPath(iosSupportPath);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                libraryPath = GetMacOsNativeLibraryPath(iosSupportPath);
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
            return Path.Combine(iosSupportPath,
                Environment.Is64BitOperatingSystem ? NativeDllWin64 : NativeDllWin32);
        }

        private static string GetMacOsNativeLibraryPath(string iosSupportPath)
        {
            string dylibPath;

            // We know we're either Intel X64, Rosetta X64 or M1 Arm64. Rosetta reports itself as X64 and needs the X64
            // native lib, so we're all good!
            var isAppleSilicon = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
            if (isAppleSilicon)
            {
                dylibPath = Path.Combine(iosSupportPath, NativeDylibOsxArm64);
                if (!File.Exists(dylibPath) && Directory.Exists(iosSupportPath))
                {
                    Console.WriteLine("No Apple Silicon native library available at '{0}'", dylibPath);
                    throw new PlatformNotSupportedException(
                        "Apple Silicon support requires a native install of Unity 2021.2 or above");
                }
            }
            else
            {
                // Unity 2021.2 moved the native library into the x64 (and arm64) directory. Use the fallback on older
                // Unity versions
                dylibPath = Path.Combine(iosSupportPath, NativeDylibOsxX64);
                if (!File.Exists(dylibPath))
                {
                    var fallbackDylibPath = Path.Combine(iosSupportPath, NativeDylibOsxFallback);
                    if (!File.Exists(fallbackDylibPath))
                    {
                        // Show where we've looked
                        Console.WriteLine("Cannot find native library: {0}", dylibPath);
                        Console.WriteLine("Cannot find native library: {0}", fallbackDylibPath);

                        // Fall through to default error handling (i.e. we'll throw FileNotFoundException when trying to
                        // load the library)
                    }
                    else
                    {
                        dylibPath = fallbackDylibPath;
                    }
                }
            }

            return dylibPath;
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