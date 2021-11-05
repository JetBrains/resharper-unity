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

            var pollingCancellationSource = new CancellationTokenSource();
            var readLineTask = CreateAwaitStopCommandTask();
            var innerPollingTask = CreatePollForDevicesTask(iosSupportPath, pollingInterval,
                pollingCancellationSource.Token);

            // ReSharper disable once MethodSupportsCancellation
            // Don't pass cancellation token, or this continuation will be cancelled before it gets a chance to run
            var pollingTask = innerPollingTask.ContinueWith(t =>
            {
                // If the polling task faults, log it, and continue with a successfully completed task
                if (t.IsFaulted && t.Exception != null)
                {
                    foreach (var e in t.Exception.InnerExceptions)
                        Console.WriteLine(e);
                }
            });

            await Task.WhenAny(readLineTask, pollingTask);
            if (readLineTask.Status != TaskStatus.Running)
            {
                pollingCancellationSource.Cancel();
                await pollingTask;
            }

            return innerPollingTask.IsFaulted ? 1 : 0;
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

        private static Task CreateAwaitStopCommandTask()
        {
            return Task.Run(() =>
            {
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line?.Equals("stop", StringComparison.OrdinalIgnoreCase) == true)
                        return;
                }
            });
        }

        private static Task CreatePollForDevicesTask(string iosSupportPath, TimeSpan pollingInterval, CancellationToken token)
        {
            // ReSharper disable once FunctionNeverReturns
            return Task.Run(() =>
            {
                using var api = new ListDevices(iosSupportPath);
                while (true)
                {
                    var devices = api.GetDevices();

                    Console.WriteLine($"{devices.Count}");
                    foreach (var device in devices)
                        Console.WriteLine($"{device.productId:X} {device.udid}");

                    Thread.Sleep(pollingInterval);
                    token.ThrowIfCancellationRequested();
                }
            }, token);
        }
    }
}
