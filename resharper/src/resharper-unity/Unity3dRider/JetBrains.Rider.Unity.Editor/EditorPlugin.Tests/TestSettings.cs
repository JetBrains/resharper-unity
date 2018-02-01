using JetBrains.Rider.Unity.Editor;

namespace EditorPlugin.Tests
{
  public class TestPluginSettings:IPluginSettings
  {
    public OperatingSystemFamilyRider OperatingSystemFamilyRider => OperatingSystemFamilyRider.Windows;

    public string RiderPath { get; set; }
  }
}