using JetBrains.Rider.Unity.Editor;
using NUnit.Framework;

namespace EditorPlugin.Tests
{
  [TestFixture]
  public class RiderPluginTest
  {
    [Test]
    public void EmptyDefaultPathTest()
    {
      Assert.IsNull(RiderApplication.GetDefaultRiderApp(string.Empty, new string[0]));
    }
    
    [Test]
    public void NullDefaultPathTest()
    {
      Assert.IsNull(RiderApplication.GetDefaultRiderApp(null, new string[0]));
    }

//    [Test]
//    public void DefaultPathTest()
//    {
//      var expected = @"C:\Rider.exe";
//      var res = RiderApplication.GetDefaultRiderApp(expected, new[] {"", expected, "B"});
//      Assert.AreEqual(expected, res);
//    }
  }
}