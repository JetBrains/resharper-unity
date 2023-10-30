using System;
using System.Collections.Generic;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Build;
using JetBrains.Rider.Backend.BuildScript.Plugins;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.EditorPlugin.BuildScript;

public class LocalDeployStep
{
    [BuildStep]
    public static IEnumerable<TransformedSubplatformFileForPackaging> DoLocalDeploy(ProductBinariesDirArtifact productBinariesDirArtifact, IEnumerable<SubplatformFileForPackaging> files, ILogger logger)
    {
        LocalDeployUtils.DeployFiles(EditorPluginProduct.SubplatformName, EditorPluginProduct.PluginFolder, files, productBinariesDirArtifact, logger);
        return Array.Empty<TransformedSubplatformFileForPackaging>();
    }
}