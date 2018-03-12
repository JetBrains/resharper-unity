using System.IO;
using EditorPlugin.Tests;
using NUnit.Framework;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  [TestFixture]
  public class RiderPluginTest
  {
    private static readonly FileInfo ourRiderPathForTests = new FileInfo(Path.Combine(Path.GetTempPath(), "Rider.exe"));
    private static readonly IPluginSettings ourTestPluginSettings = new TestPluginSettings();
    private static readonly RiderPathLocator ourRiderPathLocator = new RiderPathLocator(ourTestPluginSettings);

    [SetUp]
    public void SetUp()
    {
      if (ourRiderPathForTests.Exists)
      {
        ourRiderPathForTests.Delete();
      }
    }
    
    [Test]
    public void EmptyDefaultPathTest()
    {
      Assert.IsNull(ourRiderPathLocator.GetDefaultRiderApp(string.Empty, new string[0]));
    }
    
    [Test]
    public void NullDefaultPathTest()
    {
      Assert.IsNull(ourRiderPathLocator.GetDefaultRiderApp(null, new string[0]));
    }
    
    [Test]
    public void NullButExistRiderDefaultPathTest()
    {
      File.WriteAllText(ourRiderPathForTests.FullName, "test");
      Assert.AreEqual(ourRiderPathForTests.FullName, ourRiderPathLocator.GetDefaultRiderApp(null, new[] {ourRiderPathForTests.FullName, "B"}));
    }

    [Test]
    public void AllFoundPathsContainExternalEditorPathTest()
    {
      File.WriteAllText(ourRiderPathForTests.FullName, "test");
      var res = ourRiderPathLocator.GetDefaultRiderApp(ourRiderPathForTests.FullName, new[] {"", ourRiderPathForTests.FullName, "B"});
      Assert.AreEqual(ourRiderPathForTests.FullName, res);
    }
    
    [Test]
    public void AllFoundPathsNotContainExternalEditorPathTest()
    {
      File.WriteAllText(ourRiderPathForTests.FullName, "test");
      var res = ourRiderPathLocator.GetDefaultRiderApp(null, new[] {"", ourRiderPathForTests.FullName, "B"});
      Assert.AreEqual(ourRiderPathForTests.FullName, res);
    }
  }
}