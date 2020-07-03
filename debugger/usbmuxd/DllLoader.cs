using System;
using System.IO;
using System.Runtime.InteropServices;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.IosUsbDebugging.NativeInterop
{
    internal interface IDllLoader {
        IntPtr LoadLibrary(string fileName);
        void FreeLibrary(IntPtr handle);
        IntPtr GetProcAddress(IntPtr dllHandle, string name);
    }

    internal class WindowsDllLoader : IDllLoader
    {
        IntPtr IDllLoader.LoadLibrary(string fileName)
        {
            if (!File.Exists (fileName))
                throw new FileNotFoundException (fileName);

            return LoadLibrary(fileName);
        }

        void IDllLoader.FreeLibrary(IntPtr handle)
        {
            FreeLibrary(handle);
        }

        IntPtr IDllLoader.GetProcAddress(IntPtr dllHandle, string name)
        {
            return GetProcAddress(dllHandle, name);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32.dll")]
        private static extern int FreeLibrary(IntPtr handle);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetProcAddress (IntPtr handle, string procedureName);
    }

    internal class PosixDllLoader: IDllLoader
    {
        public IntPtr LoadLibrary(string fileName)
        {
            if (!File.Exists (fileName))
                throw new FileNotFoundException (fileName);

            // clear previous errors if any
            dlerror();
            var res = dlopen(fileName, RTLD_NOW);
            var err = dlerror();
            if (res == IntPtr.Zero) {
                throw new Exception("dlopen: " + Marshal.PtrToStringAnsi(err));
            }
            return res;
        }

        public void FreeLibrary(IntPtr handle)
        {
            dlclose(handle);
        }

        public IntPtr GetProcAddress(IntPtr dllHandle, string name)
        {
            // clear previous errors if any
            dlerror();
            var res = dlsym(dllHandle, name);
            var errPtr = dlerror();
            if (errPtr != IntPtr.Zero) {
                throw new Exception("dlsym: " + Marshal.PtrToStringAnsi(errPtr));
            }
            return res;
        }

        // ReSharper disable once UnusedMember.Local
        private const int RTLD_LAZY = 1;
        private const int RTLD_NOW = 2;

        [DllImport("libdl")]
        private static extern IntPtr dlopen(string fileName, int flags);

        [DllImport("libdl")]
        private static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("libdl")]
        private static extern int dlclose(IntPtr handle);

        [DllImport("libdl")]
        private static extern IntPtr dlerror();
    }
}