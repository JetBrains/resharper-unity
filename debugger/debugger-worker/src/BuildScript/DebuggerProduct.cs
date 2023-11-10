using System;
using JetBrains.Application.BuildScript.Compile;
using JetBrains.Application.BuildScript.PackageSpecification;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;
using JetBrains.Rider.Backend.BuildScript;
using JetBrains.Util;

namespace JetBrains.Debugger.Worker.Plugins.Unity.BuildScript
{
  /// <summary>
  ///   Defines a bundled plugin which drives adding the referenced packages as a plugin for Rider.
  /// </summary>
  public class DebuggerProduct
  {
      public static readonly SubplatformName SubplatformName = new((RelativePath)"Plugins" / "ReSharperUnity" / "debugger" / "debugger-worker");

      public static readonly RelativePath PluginFolder = @"plugins\rider-unity\dotnetDebuggerWorker";

      public const string ProductTechnicalName = "Unity.Debugger";

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
}