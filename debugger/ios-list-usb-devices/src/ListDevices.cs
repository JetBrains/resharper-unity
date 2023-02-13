using System;
using System.Collections.Generic;
using JetBrains.Debugger.Worker.Plugins.Unity;

namespace JetBrains.Rider.Plugins.Unity.iOS.ListUsbDevices
{
    public class ListDevices : IDisposable
    {
        private readonly List<Usbmuxd.iOSDevice> myDeviceIds = new();
        private readonly Usbmuxd myUsbmuxd;

        public ListDevices(string dllPath)
        {
            if (Usbmuxd.IsDllLoaded)
                throw new InvalidOperationException("Usbmuxd already initialised! Cannot initialise multiple times");

            myUsbmuxd = Usbmuxd.Create(dllPath);

            // Note that we might get an empty "[usmbuxd] Error:" message. This is harmless, and is due to shutdown handling
            myUsbmuxd.StartUsbmuxdListenThread();
        }

        public List<Usbmuxd.iOSDevice> GetDevices()
        {
            myDeviceIds.Clear();

            var count = myUsbmuxd.UsbmuxdGetDeviceCount();
            for (uint i = 0; i < count; i++)
            {
                if (myUsbmuxd.UsbmuxdGetDevice(i, out var device) && !string.IsNullOrEmpty(device.udid))
                {
                    myDeviceIds.Add(device);
                }
            }

            return myDeviceIds;
        }

        public void Dispose()
        {
            myUsbmuxd.StopUsbmuxdListenThread();
            myUsbmuxd.Shutdown();
        }
    }
}