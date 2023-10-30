using System.Collections.Generic;
using System.IO;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Build.Helpers.TeamCity;
using JetBrains.Extension;
using JetBrains.Util;
using JetBrains.Util.Storage;

namespace JetBrains.ReSharper.Plugins.Unity.EditorPlugin.BuildScript;

public class GenerateRiderDevBat
{
    [BuildStep]
    public static IEnumerable<SubplatformFileForPackaging> Generate(AllAssembliesOnEverything allass , ProductHomeDirArtifact homeDirArtifact, ILogger logger)
    {
        if (TeamCityProperties.GetIsRunningInTeamCity())
            yield break;

        if (allass.FindSubplatformByClass<CompileEditorPluginBuildStep>() is SubplatformOnSources subplatform)
        {
            yield return new SubplatformFileForPackaging(subplatform.Name, ImmutableFileItem.CreateFromStream("rider-dev.app/rider-dev.bat",
                s =>
                {
                    WritePath(homeDirArtifact.ProductHomeDir, s);
                }));
            
            yield return new SubplatformFileForPackaging(subplatform.Name, ImmutableFileItem.CreateFromStream("rider-dev.bat",
                s =>
                {
                    WritePath(homeDirArtifact.ProductHomeDir, s);
                }));
        }
    }

    private static void WritePath(FileSystemPath productHomeDir, Stream stream)
    {
        using (var sw = new StreamWriter(stream))
        {
            var currentPath = productHomeDir / "Rider" / "Frontend" / "out" / "dev-run" / "Rider" / EditorPluginProduct.PluginFolder / "JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll";
            sw.WriteLine(currentPath);
        }
    }
}