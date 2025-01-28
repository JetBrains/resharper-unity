using JetBrains.Rider.PathLocator;
using Xunit;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  public class RiderPathLocatorTest
  {
    private readonly RiderPathLocator myRiderPathLocator;
    
    public RiderPathLocatorTest()
    {
      myRiderPathLocator = new RiderPathLocator(new RiderLocatorEnvironmentInTest());
    }
    
    [Fact]
    public void Test()
    {
      var infos = myRiderPathLocator.GetAllRiderPaths();
      
      // Assert.Equal(3, infos.Length);
      // var paths = infos.Select(x => x.Presentation + " " + x.Path);
      // var res = string.Join(Environment.NewLine, paths.ToArray());
      // Assert.True(infos.Length == 3, res);
    }
  }
}