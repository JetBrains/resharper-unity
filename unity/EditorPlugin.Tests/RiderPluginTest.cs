using System.IO;
using Xunit;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  public class RiderPluginTest
  {
    private static readonly FileInfo ourRiderPathForTests = new FileInfo(Path.Combine(Path.GetTempPath(), "Rider.exe"));
    private static readonly IPluginSettings ourTestPluginSettings = new TestPluginSettings();
    private static readonly RiderPathLocator ourRiderPathLocator = new RiderPathLocator(ourTestPluginSettings);

    public RiderPluginTest()
    {
      if (ourRiderPathForTests.Exists)
      {
        ourRiderPathForTests.Delete();
      }
    }
    
    [Fact]
    public void EmptyDefaultPathTest()
    {
      Assert.Null(ourRiderPathLocator.GetDefaultRiderApp(string.Empty, new string[0]));
    }
    
    [Fact]
    public void NullDefaultPathTest()
    {
      Assert.Null(ourRiderPathLocator.GetDefaultRiderApp(null, new string[0]));
    }
    
    [Fact]
    public void NullButExistRiderDefaultPathTest()
    {
      File.WriteAllText(ourRiderPathForTests.FullName, "test");
      Assert.Equal(ourRiderPathForTests.FullName, ourRiderPathLocator.GetDefaultRiderApp(null, new[] {ourRiderPathForTests.FullName, "B"}));
    }

    [Fact]
    public void AllFoundPathsContainExternalEditorPathTest()
    {
      File.WriteAllText(ourRiderPathForTests.FullName, "test");
      var res = ourRiderPathLocator.GetDefaultRiderApp(ourRiderPathForTests.FullName, new[] {"", ourRiderPathForTests.FullName, "B"});
      Assert.Equal(ourRiderPathForTests.FullName, res);
    }
    
    [Fact(Skip = "@ivan.shakhov")]
    public void AllFoundPathsNotContainExternalEditorPathTest()
    {
      File.WriteAllText(ourRiderPathForTests.FullName, "test");
      var res = ourRiderPathLocator.GetDefaultRiderApp(null, new[] {"", ourRiderPathForTests.FullName, "B"});
      Assert.Equal(ourRiderPathForTests.FullName, res);
    }
  }
}