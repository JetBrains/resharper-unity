using System;
using System.Collections.Generic;
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

            var arguments = args.ToList();
            arguments.RemoveAt(0);

            // Note: Pass --offline to run gradle in offline mode
            if (IsWindows())
                ExecuteBatchFile("gradlew.bat", arguments);
            else
                ExecuteShellScript("./gradlew", arguments);
        }

        private static bool IsWindows()
        {
            var platform = Environment.OSVersion.Platform;
            return platform == PlatformID.Win32Windows || platform == PlatformID.Win32NT;
        }

        private static void ExecuteBatchFile(string batchFile, IEnumerable<string> args)
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

        static void ExecuteShellScript(string command, IEnumerable<string> args)
        {
            var commandline = $"-c \"{command} {string.Join(" ", args)}\"";

            Console.WriteLine($"Starting sh {commandline}");
            var processInfo = new ProcessStartInfo("sh", commandline)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var process = Process.Start(processInfo))
            {
                if (process == null)
                    throw new InvalidOperationException("Woah, process is null!");

                process.WaitForExit();
            }
        }

        private static string FormatArgs(this IEnumerable<string> args)
        {
            return string.Join(" ", args);
        }
    }
}