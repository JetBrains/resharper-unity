using System;
using System.Net.Sockets;
using System.Threading;

namespace JetBrains.Rider.Plugins.Unity.iOS.ListUsbDevices
{
    internal static class Program
    {
        private static bool ourFinished;

        private static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: ios-list-usb-devices dllFolderPath sleepInMs");
                Console.WriteLine("  Type 'stop' to finish");
                return -1;
            }

            InitialiseWinSock();

            var thread = new Thread(ThreadFunc);
            thread.Start(args);

            while (true)
            {
                if (Console.ReadLine()?.Equals("stop", StringComparison.OrdinalIgnoreCase) == true)
                {
                    ourFinished = true;
                    break;
                }
            }

            thread.Join();

            return 0;
        }

        private static void InitialiseWinSock()
        {
            // Small hack to force WinSock to initialise on Windows. If we don't do this, the C based socket APIs in the
            // native dll will fail, because no-one has bothered to initialise sockets.
            try
            {
                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                socket.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to create socket (force initialising WinSock on Windows)");
                Console.WriteLine(e);
            }
        }

        private static void ThreadFunc(object state)
        {
            var args = (string[]) state;
            var sleepTimeMs = int.Parse(args[1]);
            using (var api = new ListDevices(args[0]))
            {
                while (!ourFinished)
                {
                    var devices = api.GetDevices();

                    Console.WriteLine($"{devices.Count}");
                    foreach (var device in devices)
                        Console.WriteLine($"{device.productId:X} {device.udid}");

                    Thread.Sleep(sleepTimeMs);
                }
            }
        }
    }
}
