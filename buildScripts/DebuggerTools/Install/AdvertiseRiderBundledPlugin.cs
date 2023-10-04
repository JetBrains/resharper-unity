using JetBrains.Build;
using JetBrains.ReSharper.Plugins.Unity.DebuggerTools.BuildScript;
using JetBrains.Rider.Backend.Install;

namespace JetBrains.ReSharper.Plugins.Unity.DebuggerTools.Install
{
  public static class AdvertiseRiderBundledPlugin
  {
    [BuildStep]
    public static RiderBundledProductArtifact[] ShipUnityWithRider()
    {
      return new[]
      {
        new RiderBundledProductArtifact(
            DebuggerToolsProduct.ProductTechnicalName,
            DebuggerToolsProduct.SubplatformName,
            DebuggerToolsProduct.PluginFolder,
          allowCommonPluginFiles: false),
      };
    }
  }
}