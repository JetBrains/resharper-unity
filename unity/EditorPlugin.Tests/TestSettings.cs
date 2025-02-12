using JetBrains.Rider.PathLocator;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  internal class TestPluginSettings : IPluginSettings
  {
    public OS OSRider => PluginSettings.SystemInfoRiderPlugin.OS;

    public string RiderPath { get; set; }
  }
}