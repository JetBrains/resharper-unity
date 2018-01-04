using JetBrains.Rider.Unity.Editor;

namespace EditorPlugin.Tests
{
  public class TestPluginSettings:IPluginSettings
  {
    public OperatingSystemFamilyRider OperatingSystemFamilyRider
    {
      get { return OperatingSystemFamilyRider.Windows; }
    }

    public string RiderPath { get; set; }
  }
}