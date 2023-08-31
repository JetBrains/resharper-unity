using System;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.PackageSpecification;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Rider.Backend.BuildScript;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.BuildScript
{
  /// <summary>
  ///   Defines a bundled plugin which drives adding the referenced packages as a plugin for Rider.
  /// </summary>
  public class UnityInRiderProduct
  {
    public static readonly SubplatformName ThisSubplatformName = new((RelativePath)"Plugins" / "ReSharperUnity" / "resharper" / "resharper-unity" / "src" / "Unity.Rider");

    public static readonly RelativePath DotFilesFolder = @"plugins\rider-unity\dotnet";

    public const string ProductTechnicalName = "Unity";

    [BuildStep]
    public static SubplatformComponentForPackagingFast[] ProductMetaDependency(AllAssembliesOnSources allassSrc)
    {
      if (!allassSrc.Has(ThisSubplatformName))
        return Array.Empty<SubplatformComponentForPackagingFast>();

      return new[]
      {
        new SubplatformComponentForPackagingFast
        (
          ThisSubplatformName,
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

    public class Debugger
    {
      public static readonly SubplatformName DebuggerSubplatformName = new((RelativePath)"Plugins" / "ReSharperUnity" / "debugger" / "debugger-worker");

      public static readonly RelativePath DebuggerFolder = @"plugins\rider-unity\dotnetDebuggerWorker";

      public const string ProductTechnicalName = "Unity.Debugger";

      [BuildStep]
      public static SubplatformComponentForPackagingFast[] ProductMetaDependency(AllAssembliesOnSources allassSrc)
      {
        if (!allassSrc.Has(DebuggerSubplatformName))
            return Array.Empty<SubplatformComponentForPackagingFast>();

        return new[]
        {
          new SubplatformComponentForPackagingFast
          (
            DebuggerSubplatformName,
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
  }
}