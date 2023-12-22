using JetBrains.Rider.PathLocator;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  internal class TestPluginSettings : IPluginSettings
  {
    public OS OSRider => OS.Windows;

    public string RiderPath { get; set; }
  }
}