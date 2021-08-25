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

        // On my Windows 2019.3.5f1 install, this file is x86_64\\UnityEditor.iOS.Native.dll
        // 5.6.7f1 (Mac), the file is UnityEditor.iOS.Native.dylib. Will need to check on Windows
        // 5.6.7f1 (Windows) => UnityEditor.iOS.Native.dll
        private const string NativeDllOsx = "UnityEditor.iOS.Native.dylib";
        private const string NativeDllWin32 = "x86\\UnityEditor.iOS.Native.dll";
        private const string NativeDllWin64 = "x86_64\\UnityEditor.iOS.Native.dll";

        private static readonly IDllLoader ourLoader;
        private static IntPtr ourDllHandle;

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
        public static bool IsDllLoaded => ourDllHandle != IntPtr.Zero;

        private static void LoadDll(string nativeDll)
        {
            if (ourDllHandle == IntPtr.Zero)
            {
                ourDllHandle = ourLoader.LoadLibrary(nativeDll);
                if (ourDllHandle != IntPtr.Zero)
                    Console.WriteLine("Loaded: " + nativeDll);
                else
                    Console.WriteLine("Couldn't load: " + nativeDll);
            }
        }

        private static TType LoadFunction<TType>(string name) where TType: class
        {
            if (ourDllHandle == IntPtr.Zero)
                throw new Exception("iOS native extension dll was not loaded");

            IntPtr addr = ourLoader.GetProcAddress(ourDllHandle, name);
            return Marshal.GetDelegateForFunctionPointer(addr, typeof(TType)) as TType;
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

        public static void Setup(string dllPath)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                case PlatformID.Unix:
                    dllPath = Path.Combine(dllPath, NativeDllOsx);
                    break;

                case PlatformID.Win32NT:
                    dllPath = Path.Combine(dllPath,
                        Environment.Is64BitOperatingSystem ? NativeDllWin64 : NativeDllWin32);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            LoadDll(dllPath);
            InitFunctions();
        }

        public static void Shutdown()
        {
            if (ourDllHandle == IntPtr.Zero)
                ourLoader.FreeLibrary(ourDllHandle);
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
                    throw new NotSupportedException("Platform not supported");
            }
        }
    }
}