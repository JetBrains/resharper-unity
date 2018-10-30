using System;
using System.Diagnostics;
using System.Linq;

namespace GradleLauncher
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: GradleLauncher workingDirectory args");
                return;
            }

            Environment.CurrentDirectory = args[0];
            Console.WriteLine("Current directory: {0}", Environment.CurrentDirectory);

            // Don't perform an online dependency check every time we try to run. We've already done this when initially
            // building the project, and it's just dead annoying when you're on a plane
            args[0] = "--offline";

            if (IsWindows())
                ExecuteBatchFile("gradlew.bat", args);
            else
                ExecuteShellScript("./gradlew", args);
        }

        private static bool IsWindows()
        {
            var platform = Environment.OSVersion.Platform;
            return platform == PlatformID.Win32Windows || platform == PlatformID.Win32NT;
        }

        private static void ExecuteBatchFile(string batchFile, string[] args)
        {
            var commandline = args.FormatArgs();

            Console.WriteLine($"Starting {batchFile} {commandline}");

            var processStartInfo = new ProcessStartInfo(batchFile, commandline)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            using (var process = Process.Start(processStartInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("Woah, process is null!");

                process.OutputDataReceived += (sender, e) =>
                    Console.WriteLine(e.Data);
                process.BeginOutputReadLine();

                process.ErrorDataReceived += (sender, e) =>
                    Console.Error.WriteLine(e.Data);
                process.BeginErrorReadLine();

                process.WaitForExit();
            }
        }

        static void ExecuteShellScript(string command, string[] args)
        {
            var commandline = $"-c \"{command} {string.Join(" ", args)}\"";

            Console.WriteLine($"Starting sh {commandline}");
            var processInfo = new ProcessStartInfo("sh", commandline)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(processInfo))
                process.WaitForExit();
        }

        static string FormatArgs(this string[] args)
        {
            return string.Join(" ", args);
        }
    }
}