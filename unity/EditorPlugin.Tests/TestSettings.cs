namespace JetBrains.Rider.Unity.Editor.Tests
{
  internal class TestPluginSettings : IPluginSettings
  {
    public OperatingSystemFamilyRider OperatingSystemFamilyRider => OperatingSystemFamilyRider.Windows;

    public string RiderPath { get; set; }
  }
}