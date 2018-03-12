namespace JetBrains.Rider.Unity.Editor.Tests
{
  public class TestPluginSettings:IPluginSettings
  {
    public OperatingSystemFamilyRider OperatingSystemFamilyRider => OperatingSystemFamilyRider.Windows;

    public string RiderPath { get; set; }
  }
}