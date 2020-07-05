using System;
using System.Threading;
using JetBrains.Debugger.Worker.SessionStartup;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.IosUsbDebugging.NativeInterop;
using JetBrains.Rider.Model.DebuggerWorker;
using JetBrains.Util;
using Mono.Debugging.Autofac;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.IosUsbDebugging
{
    // The only components that are created during startup are IModelStartInfoHandler. We don't actually do anything
    // with this interface, just take advantage of the lifecycle, to call initialise
    [DebuggerGlobalComponent]
    public class IosUsbProxyLoader : IInitializable, IModelStartInfoHandler
    {
        private readonly Lifetime myLifetime;
        private readonly ILogger myLogger;

        public IosUsbProxyLoader(Lifetime lifetime, ILogger logger)
        {
            myLifetime = lifetime;
            myLogger = logger;

            myLogger.Trace("IosUsbProxyLoader::ctor");
        }

        public void Initialize()
        {
            // TODO: Investigate using StartInfo and the protocol for this information
            var proxyPath = Environment.GetEnvironmentVariable("_RIDER_UNITY_IOS_USB_PROXY_PATH");
            var deviceID = Environment.GetEnvironmentVariable("_RIDER_UNITY_IOS_USB_DEVICE_ID");
            var localPortString = Environment.GetEnvironmentVariable("_RIDER_UNITY_IOS_USB_LOCAL_PORT");

            if (!proxyPath.IsNullOrEmpty() && !deviceID.IsNullOrEmpty() && !localPortString.IsNullOrEmpty())
            {
                myLogger.Trace($"Proxy path: {proxyPath}");
                myLogger.Trace($"Device ID: {deviceID}");
                myLogger.Trace($"Local port: {localPortString}");

                try
                {
                    // Load the native library, initialise the function pointers and start listening for devices
                    Usbmuxd.Setup(proxyPath);
                    Usbmuxd.StartUsbmuxdListenThread();

                    // There is a potential race condition with starting the proxy thread before the listen thread has
                    // discovered the devices. Make sure our device ID is found
                    var retries = 0;
                    bool found;
                    while ((found = CanFindDevice(deviceID)) == false && retries < 3)
                    {
                        myLogger.Info("Cannot find device. Sleeping for 10ms");
                        Thread.Sleep(10);
                        retries++;
                    }

                    // This shouldn't happen. Log it and let everything else continue to fail
                    if (!found) myLogger.Error("Unable to find device");

                    var localPort = ushort.Parse(localPortString);
                    if (!Usbmuxd.StartIosProxy(localPort, 56000, deviceID))
                    {
                        myLogger.Error("StartIosProxy returned false");
                        Usbmuxd.StopUsbmuxdListenThread();
                        Usbmuxd.Shutdown();
                        return;
                    }

                    myLifetime.OnTermination(() =>
                    {
                        try
                        {
                            Usbmuxd.StopIosProxy(localPort);
                            Usbmuxd.StopUsbmuxdListenThread();
                            Usbmuxd.Shutdown();
                        }
                        catch (Exception e)
                        {
                            myLogger.Error(e);
                            throw;
                        }
                    });
                }
                catch (Exception e)
                {
                    myLogger.Error(e);
                    throw;
                }
            }
            else
            {
                myLogger.Trace("No environment variables set for iOS debugging");
            }
        }

        public StartInfo GetStartInfo(Lifetime lifetime, DebuggerStartInfoBase modelStartInfo, SessionProperties properties)
        {
            return StartInfo.Empty;
        }

        private bool CanFindDevice(string deviceId)
        {
            var deviceCount = Usbmuxd.UsbmuxdGetDeviceCount();

            myLogger.Trace($"UsbmuxdGetDeviceCount: {deviceCount}");
            for (uint i = 0; i < deviceCount; i++)
            {
                if (Usbmuxd.UsbmuxdGetDevice(i, out var device))
                {
                    myLogger.Trace($"UsbmuxdGetDevice({i}): {device.udid}");
                    if (device.udid == deviceId)
                        return true;
                }
            }

            return false;
        }
    }
}