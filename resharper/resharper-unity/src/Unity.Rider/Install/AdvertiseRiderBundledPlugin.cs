using JetBrains.Build;
using JetBrains.ReSharper.Plugins.Unity.Rider.BuildScript;
using JetBrains.Rider.Backend.Install;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Install
{
  public static class AdvertiseRiderBundledPlugin
  {
    [BuildStep]
    public static RiderBundledProductArtifact[] ShipUnityWithRider()
    {
      return new[]
      {
        new RiderBundledProductArtifact(
          UnityInRiderProduct.ProductTechnicalName,
          UnityInRiderProduct.ThisSubplatformName,
          UnityInRiderProduct.DotFilesFolder,
          allowCommonPluginFiles: false)
        
      };
    }
  }
}