using System;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.PackageSpecification;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Rider.Backend.BuildScript;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.EditorPlugin.BuildScript;

public class EditorPluginProduct
{
    public static readonly SubplatformName SubplatformName = new((RelativePath)"Plugins" / "ReSharperUnity" / "buildScripts" / "EditorPlugin");

    public static readonly RelativePath PluginFolder = @"plugins\rider-unity\EditorPlugin";

    public const string ProductTechnicalName = "Unity.EditorPlugin";

    [BuildStep]
    public static SubplatformComponentForPackagingFast[] ProductMetaDependency(AllAssembliesOnSources allassSrc)
    {
        if (!allassSrc.Has(SubplatformName))
            return Array.Empty<SubplatformComponentForPackagingFast>();

        return new[]
        {
            new SubplatformComponentForPackagingFast
            (
                SubplatformName,
                new JetPackageMetadata
                {
                    Spec = new JetSubplatformSpec
                    {
                        ComplementedProductName = RiderConstants.ProductTechnicalName
                    }
                }
            )
        };
    }
}
