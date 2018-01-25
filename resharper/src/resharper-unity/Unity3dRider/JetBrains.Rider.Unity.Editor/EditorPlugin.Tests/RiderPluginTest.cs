using System;
using System.IO;
using JetBrains.Rider.Unity.Editor;
using NUnit.Framework;

namespace EditorPlugin.Tests
{
  [TestFixture]
  public class RiderPluginTest
  {
    private static readonly FileInfo riderPathForTests = new FileInfo(Path.Combine(Path.GetTempPath(), "Rider.exe"));
    private static readonly IPluginSettings TestPluginSettings = new TestPluginSettings();
    private static readonly RiderApplication ourRiderApplication = new RiderApplication(TestPluginSettings);

    [SetUp]
    public void SetUp()
    {
      if (riderPathForTests.Exists)
      {
        riderPathForTests.Delete();
      }
    }
    
    [Test]
    public void EmptyDefaultPathTest()
    {
      Assert.IsNull(ourRiderApplication.GetDefaultRiderApp(string.Empty, new string[0]));
    }
    
    [Test]
    public void NullDefaultPathTest()
    {
      Assert.IsNull(ourRiderApplication.GetDefaultRiderApp(null, new string[0]));
    }
    
    [Test]
    public void NullButExistRiderDefaultPathTest()
    {
      File.WriteAllText(riderPathForTests.FullName, "test");
      Assert.AreEqual(riderPathForTests.FullName, ourRiderApplication.GetDefaultRiderApp(null, new[] {riderPathForTests.FullName, "B"}));
    }

    [Test]
    public void AllFoundPathsContainExternalEditorPathTest()
    {
      File.WriteAllText(riderPathForTests.FullName, "test");
      var res = ourRiderApplication.GetDefaultRiderApp(riderPathForTests.FullName, new[] {"", riderPathForTests.FullName, "B"});
      Assert.AreEqual(riderPathForTests.FullName, res);
    }
    
    [Test]
    public void AllFoundPathsNotContainExternalEditorPathTest()
    {
      File.WriteAllText(riderPathForTests.FullName, "test");
      var res = ourRiderApplication.GetDefaultRiderApp(null, new[] {"", riderPathForTests.FullName, "B"});
      Assert.AreEqual(riderPathForTests.FullName, res);
    }
  }
}