using System;
using System.Collections.Generic;
using JetBrains.Application.BuildScript;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Build;
using JetBrains.Rider.Backend.BuildScript.Plugins;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.DebuggerTools.BuildScript;

public class LocalDeployStep
{
    [BuildStep]
    public static IEnumerable<TransformedSubplatformFileForPackaging> DoLocalDeploy(ProductBinariesDirArtifact productBinariesDirArtifact, IEnumerable<SubplatformFileForPackaging> files, ILogger logger)
    {
        LocalDeployUtils.DeployFiles(DebuggerToolsProduct.SubplatformName, DebuggerToolsProduct.PluginFolder, files, productBinariesDirArtifact, logger);
        return Array.Empty<TransformedSubplatformFileForPackaging>();
    }
}