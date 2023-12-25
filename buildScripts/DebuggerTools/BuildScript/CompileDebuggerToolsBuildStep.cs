using System;
using System.Collections.Generic;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Util;
using JetBrains.Util.Storage;

namespace JetBrains.ReSharper.Plugins.Unity.DebuggerTools.BuildScript;

public class CompileDebuggerToolsBuildStep
{
    [BuildStep]
    public static IEnumerable<SubplatformFileForPackaging> CompileEditorPlugin(AllAssembliesOnEverything allAss , ProductHomeDirArtifact homeDirArtifact, ILogger logger)
    {
        if (allAss.FindSubplatformByClass<CompileDebuggerToolsBuildStep>() is SubplatformOnSources subplatform)
        {
            var dotnetSdkScript = homeDirArtifact.ProductHomeDir / "DevKit" / "Scripts" / "dotnet-sdk.cmd";
            logger.Info($"Path to dotnet-sdk: {dotnetSdkScript.FullPath}, exists: {dotnetSdkScript.ExistsFile}");

            var processBuilder = new CommandLineBuilderJet();
        
            if (PlatformUtil.IsRunningUnderWindows)
                processBuilder.AppendSwitch("/C");

            var solution = homeDirArtifact.ProductHomeDir / "Plugins" / "ReSharperUnity" / "debugger" / "DebuggerTools.sln";
            processBuilder.AppendFileName(dotnetSdkScript.FullPath)
                .AppendSwitch("build")
                .AppendSwitch("-c")
                .AppendSwitch("Release")
                .AppendSwitch("--force")
                .AppendSwitch("--no-incremental")
                .AppendFileName(solution);
        
            var shellName = PlatformUtil.IsRunningUnderWindows
                ? FileSystemPath.Parse("C:\\Windows\\System32\\cmd.exe")
                : FileSystemPath.Parse("/bin/sh");

            logger.Info("Start DebuggerTools compilation...");
            var result = InvokeChildProcess.InvokeChildProcessIntoLogger(shellName, processBuilder);
            if (result != 0)
            {
                logger.Error($"Failed to compile DebuggerTools. Open '{solution.FullPath}' and fix errors");
            }
        
            logger.Info("Finished DebuggerTools compilation without errors");

            var iosOutputFolder = homeDirArtifact.ProductHomeDir / "Plugins" / "ReSharperUnity" / "resharper" / "build" / "ios-list-usb-devices" / "bin" / "Release" / "net7.0";
            var textureUtilsOutputFolder = homeDirArtifact.ProductHomeDir / "Plugins" / "ReSharperUnity" / "resharper" / "build" / "texture-debugger" / "bin" / "Release" / "net472";
        
            // TODO sign artifacts
            
            return new SubplatformFileForPackaging[]
            {
                new(subplatform.Name, ImmutableFileItem.CreateFromDisk(iosOutputFolder / "JetBrains.Rider.Unity.ListIosUsbDevices.dll")),
                new(subplatform.Name, ImmutableFileItem.CreateFromDisk(iosOutputFolder / "JetBrains.Rider.Unity.ListIosUsbDevices.pdb")),
                new(subplatform.Name, ImmutableFileItem.CreateFromDisk(iosOutputFolder / "JetBrains.Rider.Unity.ListIosUsbDevices.runtimeconfig.json")),

                new(subplatform.Name, ImmutableFileItem.CreateFromDisk(textureUtilsOutputFolder / "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.dll")),
                new(subplatform.Name, ImmutableFileItem.CreateFromDisk(textureUtilsOutputFolder / "JetBrains.ReSharper.Plugins.Unity.Rider.Debugger.Presentation.Texture.pdb")),
            };
        }

        return Array.Empty<SubplatformFileForPackaging>();
    }
}