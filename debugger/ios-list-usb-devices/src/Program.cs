using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace JetBrains.Rider.Plugins.Unity.iOS.ListUsbDevices
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: ios-list-usb-devices dllFolderPath sleepInMs");
                Console.WriteLine("  Type 'stop' to finish");
                return -1;
            }

            InitialiseWinSock();

            var iosSupportPath = args[0];
            var pollingInterval = TimeSpan.FromMilliseconds(int.Parse(args[1]));

            var cancellationToken = StartAwaitStopCommandBackgroundTask();
            try
            {
                using var api = new ListDevices(iosSupportPath);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var devices = api.GetDevices();

                    Console.WriteLine($"{devices.Count}");
                    foreach (var device in devices)
                        Console.WriteLine($"{device.productId:X} {device.udid}");

                    await Task.Delay(pollingInterval, cancellationToken);
                }
            }
            catch (Exception e)
            {
                if (e is TaskCanceledException)
                    return 0;

                Console.WriteLine(e);
                return 1;
            }

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

        private static CancellationToken StartAwaitStopCommandBackgroundTask()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            // ReSharper disable once MethodSupportsCancellation
            Task.Run(() =>
            {
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line?.Equals("stop", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        cancellationTokenSource.Cancel();
                        return;
                    }
                }
            });

            return cancellationTokenSource.Token;
        }
    }
}
