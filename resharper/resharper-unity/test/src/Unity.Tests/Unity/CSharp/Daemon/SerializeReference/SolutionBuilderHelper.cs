using System.IO;
using System.Text;
using JetBrains.Diagnostics;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.SerializeReference
{
    internal static class SolutionBuilderHelper
    {
        private class StdErrCollector
        {
            private readonly bool myWriteStdOutInTrace;
            private readonly StringBuilder myStringBuilder = new();

            public StdErrCollector(bool writeStdOutInTrace = true)
            {
                myWriteStdOutInTrace = writeStdOutInTrace;
            }

            public string StdErr => myStringBuilder.ToString();

            public InvokeChildProcess.PumpStreamHighLevelDelegate Collect => (chunk, isStderrNotStdout, logger) =>
            {
                if (isStderrNotStdout)
                {
                    myStringBuilder.Append(chunk);
                }
                else if (myWriteStdOutInTrace)
                {
                    logger.Trace(chunk);
                }
            };
        }

        public static void PrepareDependencies(FileSystemPath baseTestDataPath, FileSystemPath testSolutionAbsolutePath, string dependencySolutionName, string outputDirectoryName)
        {
            const string DotNetSdkCmdBootstrapperFileName = "dotnet-sdk.cmd";
            var fileName = baseTestDataPath.Parent.Combine(DotNetSdkCmdBootstrapperFileName);

            PlatformUtil.ChModToExecute(fileName);

            var testSolutionDirectory = testSolutionAbsolutePath.Directory;
            var assembliesDirectoryAbsolutePath = testSolutionDirectory.Parent.Combine(outputDirectoryName);
            Directory.CreateDirectory(assembliesDirectoryAbsolutePath.ToString());
            var dependencyDirectoryAbsolutePath = testSolutionDirectory.Parent.Combine(dependencySolutionName);
            var commandLineBuilderJet = new CommandLineBuilderJet();
            if (PlatformUtil.IsRunningUnderWindows)
                commandLineBuilderJet.AppendSwitch("/C");

            commandLineBuilderJet.AppendFileName(fileName)
                .AppendSwitch("build")
                .AppendSwitch("--force")
                .AppendSwitch("--no-incremental")
                .AppendFileName(dependencyDirectoryAbsolutePath)
                .AppendSwitch("-o")
                .AppendFileName(assembliesDirectoryAbsolutePath);

            var collector = new StdErrCollector();

            var shellName = PlatformUtil.IsRunningUnderWindows
                ? FileSystemPath.Parse("C:\\Windows\\System32\\cmd.exe")
                : FileSystemPath.Parse("/bin/sh");

            var result = InvokeChildProcess.InvokeSync(shellName, commandLineBuilderJet, InvokeChildProcess.PipeStreams.Custom(collector.Collect));
            Assertion.Require(result == 0, $"Building dependencies '{dependencySolutionName}' failed, exit code: {result}.\nSTDERR: {collector.StdErr}");
        }
    }
}