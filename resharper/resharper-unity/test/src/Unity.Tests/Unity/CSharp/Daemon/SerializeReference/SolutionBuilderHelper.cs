using System.IO;
using JetBrains.Diagnostics;
using JetBrains.Rider.Backend.Features.DeploymentHost.DeploymentProviders.Uwp;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.SerializeReference
{
    internal static class SolutionBuilderHelper
    {
        public static void PrepareDependencies(FileSystemPath baseTestDataPath, FileSystemPath testSolutionAbsolutePath, string dependencySolutionName, string outputDirectoryName)
        {
            const string DotNetSdkCmdBootstrapperFileName = "dotnet-sdk.cmd";
            var fileName = baseTestDataPath.Parent.Combine(DotNetSdkCmdBootstrapperFileName);

            PlatformUtil.ChModToExecute(fileName);

            var testSolutionDirectory = testSolutionAbsolutePath.Directory;
            var assembliesDirectoryAbsolutePath = testSolutionDirectory.Parent.Combine(outputDirectoryName);
            Directory.CreateDirectory(assembliesDirectoryAbsolutePath.ToString());
            var dependencyDirectoryAbsolutePath = testSolutionDirectory.Parent.Combine(dependencySolutionName);
            var commandLineBuilderJet = new CommandLineBuilderJet()
                .AppendFileName(fileName)
                .AppendSwitch("build")
                .AppendSwitch("--force")
                .AppendSwitch("--no-incremental")
                .AppendFileName(dependencyDirectoryAbsolutePath)
                .AppendSwitch("-o")
                .AppendFileName(assembliesDirectoryAbsolutePath);

            var collector = new StdErrCollector();

            var shellName = PlatformUtil.IsRunningUnderWindows
                ? FileSystemPath.Parse("cmd.exe")
                : FileSystemPath.Parse("/bin/sh");
            
            var result = InvokeChildProcess.InvokeSync(shellName, commandLineBuilderJet, InvokeChildProcess.PipeStreams.Custom(collector.Collect));
            Assertion.Require(result == 0, $"Building dependencies '{dependencySolutionName}' failed, exit code: {result}.\nSTDERR: {collector.StdErr}");
        }
    }
}