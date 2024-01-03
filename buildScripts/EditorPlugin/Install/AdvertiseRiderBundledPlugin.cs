using JetBrains.Build;
using JetBrains.ReSharper.Plugins.Unity.EditorPlugin.BuildScript;
using JetBrains.Rider.Backend.Install;

namespace JetBrains.ReSharper.Plugins.Unity.EditorPlugin.Install
{
  public static class AdvertiseRiderBundledPlugin
  {
    [BuildStep]
    public static RiderBundledProductArtifact[] ShipUnityWithRider()
    {
      return new[]
      {
        new RiderBundledProductArtifact(
            EditorPluginProduct.ProductTechnicalName,
            EditorPluginProduct.SubplatformName,
            EditorPluginProduct.PluginFolder,
          allowCommonPluginFiles: false),
      };
    }
  }
}