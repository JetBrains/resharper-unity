using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Refasmer;
using JetBrains.Refasmer.Filters;
using Microsoft.Extensions.Logging;

namespace TestDataGenerator
{
    class Program
    {
        private static LoggerBase ourLogger = new LoggerBase(new VerySimpleLogger(Console.Error, LogLevel.Error));
        static void Main(string[] args)
        {
            GenerateNupkgFrom("NUGET_PATH", "JetBrains.Resharper.Unity.TestDataLibs", "VERSION", "ENGINE_LOCATION", "OUTPUT_FOLDER");
        }

        public static void GenerateNupkgFrom(string nugetExePath, string packageName, string version, string engineLocation, string output)
        {
            var template = GetNuspecTemplate().Replace("$version$", version).Replace("$name$", packageName);

            var packagePath = Path.Combine(output, packageName, version);
            var outputPath = Path.Combine(packagePath, "lib");
            var nuspecPath = Path.Combine(packagePath, packageName + ".nuspec");
            
            Directory.CreateDirectory(packagePath);
            File.WriteAllText(nuspecPath, template);
            
            foreach (var filePath in Directory.GetFiles(engineLocation, "*.dll"))
            {
                var name = Path.GetFileName(filePath);
                if (!name.StartsWith("UnityEngine") && !name.StartsWith("UnityEditor"))
                    continue;

                Directory.CreateDirectory(outputPath);
                MetadataImporter.MakeRefasm(filePath, Path.Combine(outputPath, name), ourLogger, new AllowAll());
            }

            var process = Process.Start(nugetExePath, $"pack {nuspecPath} -OutputDirectory {Path.Combine(output, "nupkgs", packageName)}");
            process.WaitForExit();
        }

        private static string GetNuspecTemplate()
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TestDataGenerator.nuspec.template"))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        
        public class VerySimpleLogger: ILogger
        {
            private readonly TextWriter _writer;
            private readonly LogLevel _level;
        

            public VerySimpleLogger(Stream stream, LogLevel level = LogLevel.Trace)
            {
                _level = level;
                _writer = new StreamWriter(stream);
            }

            public VerySimpleLogger(TextWriter writer, LogLevel level = LogLevel.Trace)
            {
                _writer = writer;
                _level = level;

            }
        
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (IsEnabled(logLevel))
                    _writer.WriteLine(formatter(state, exception));
            }

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _level;

            public IDisposable BeginScope<TState>(TState state) => null;
        }
    }
}