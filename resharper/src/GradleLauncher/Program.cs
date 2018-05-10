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

            var args2 = args.ToList();
            args2.RemoveAt(0);
            var arguments = args2.ToArray();

            if (Environment.OSVersion.Platform == PlatformID.Win32Windows)
                ExecuteBatchFile("gradlew.bat", arguments);
            else
                ExecuteShellScript("./gradlew", arguments);
        }

        static void ExecuteBatchFile(string command, string[] args)
        {
            var commandline = $"/c {command} {args.FormatArgs()}";
            ExecuteScriptProcessor("cmd.exe", commandline);

        }

        static void ExecuteShellScript(string command, string[] args)
        {
            var commandline = $"-c \"{command} {string.Join(" ", args)}\"";
            ExecuteScriptProcessor("sh", commandline);
        }

        static void ExecuteScriptProcessor(string processor, string commandline)
        {
            Console.WriteLine($"Starting {processor} {commandline}");
            var processInfo = new ProcessStartInfo(processor, commandline)
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