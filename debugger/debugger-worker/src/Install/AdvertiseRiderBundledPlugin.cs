using JetBrains.Build;
using JetBrains.Debugger.Worker.Plugins.Unity.BuildScript;
using JetBrains.Rider.Backend.Install;

namespace JetBrains.Debugger.Worker.Plugins.Unity.Install
{
  public static class AdvertiseRiderBundledPlugin
  {
    [BuildStep]
    public static RiderBundledProductArtifact[] ShipUnityWithRider()
    {
      return new[]
      {
        new RiderBundledProductArtifact(
          DebuggerProduct.ProductTechnicalName,
          DebuggerProduct.SubplatformName,
          DebuggerProduct.PluginFolder,
          allowCommonPluginFiles: false)
      };
    }
  }
}