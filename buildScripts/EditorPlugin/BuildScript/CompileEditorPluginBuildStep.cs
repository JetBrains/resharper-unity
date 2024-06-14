using System;
using System.Collections.Generic;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Util;
using JetBrains.Util.Storage;

namespace JetBrains.ReSharper.Plugins.Unity.EditorPlugin.BuildScript;

public class CompileEditorPluginBuildStep
{
    [BuildStep]
    public static IEnumerable<SubplatformFileForPackaging> CompileEditorPlugin(
        AllAssembliesOnEverything allAss, ProductHomeDirArtifact homeDirArtifact, ILogger logger)
    {
        if (allAss.FindSubplatformByClass<CompileEditorPluginBuildStep>() is SubplatformOnSources subplatform)
        {
            var dotnetSdkScript = homeDirArtifact.ProductHomeDir / "DevKit" / "Scripts" / "dotnet-sdk.cmd";
            logger.Info($"Path to dotnet-sdk: {dotnetSdkScript.FullPath}, exists: {dotnetSdkScript.ExistsFile}");

            var processBuilder = new CommandLineBuilderJet();
        
            if (PlatformUtil.IsRunningUnderWindows)
                processBuilder.AppendSwitch("/C");

            var solution = homeDirArtifact.ProductHomeDir 
                           / "Plugins" / "ReSharperUnity" / "unity" / "JetBrains.Rider.Unity.Editor.sln";
            processBuilder.AppendFileName(dotnetSdkScript.FullPath)
                .AppendSwitch("build")
                .AppendSwitch("-c")
                .AppendSwitch("Release")
                .AppendSwitch("--force")
                .AppendSwitch("--no-incremental")
                .AppendSwitch("-p:InternalBuild=true")
                .AppendFileName(solution);
        
            var shellName = PlatformUtil.IsRunningUnderWindows
                ? FileSystemPath.Parse("C:\\Windows\\System32\\cmd.exe")
                : FileSystemPath.Parse("/bin/sh");

            logger.Info("Start EditorPlugin compilation...");
            var result = InvokeChildProcess.InvokeChildProcessIntoLogger(shellName, processBuilder);
            if (result != 0)
            {
                logger.Error($"Failed to compile EditorPlugin. Open '{solution.FullPath}' and fix errors");
            }
        
            logger.Info("Finished EditorPlugin compilation without errors");

            var outputFolder = homeDirArtifact.ProductHomeDir / "Plugins" / "ReSharperUnity" / "unity" / "build";
        
            // TODO sign artifacts
            
            return new SubplatformFileForPackaging[]
            {
                new(subplatform.Name, ImmutableFileItem.CreateFromDisk(outputFolder / "EditorPlugin.SinceUnity.2019.2"
                    / "bin" / "Release" / "netstandard2.0" / "JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll")),
                new(subplatform.Name, ImmutableFileItem.CreateFromDisk(outputFolder / "EditorPlugin.SinceUnity.2019.2"
                    / "bin" / "Release" / "netstandard2.0" / "JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.pdb")),
            };
        }

        return Array.Empty<SubplatformFileForPackaging>();
    }
}