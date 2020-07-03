using System;
using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.IosUsbDebugging.NativeInterop;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.iOS.ListUsbDevices
{
    public class ListDevices : IDisposable
    {
        private readonly List<Usbmuxd.iOSDevice> myDeviceIds = new List<Usbmuxd.iOSDevice>();

        public ListDevices(string dllPath)
        {
            if (Usbmuxd.IsDllLoaded)
                throw new InvalidOperationException("Usbmuxd already initialised! Cannot initialise multiple times");

            Usbmuxd.Setup(dllPath);

            // Note that we might get an empty "[usmbuxd] Error:" message. This is harmless, and is due to shutdown handling
            Usbmuxd.StartUsbmuxdListenThread();
        }

        public List<Usbmuxd.iOSDevice> GetDevices()
        {
            myDeviceIds.Clear();

            var count = Usbmuxd.UsbmuxdGetDeviceCount();
            for (uint i = 0; i < count; i++)
            {
                if (Usbmuxd.UsbmuxdGetDevice(i, out var device) && !string.IsNullOrEmpty(device.udid))
                {
                    myDeviceIds.Add(device);
                }
            }

            return myDeviceIds;
        }

        public void Dispose()
        {
            Usbmuxd.StopUsbmuxdListenThread();
            Usbmuxd.Shutdown();
        }
    }
}