using System;
using JetBrains.Rider.Unity.Editor.AssetPostprocessors;
using Xunit;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  public class SlnAssetPostprocessorTest
  {
    // [Fact]
    public void TestFixRandomGuid()
    {
      var input =
        "Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Shared\", \"Shared\", \"{8FCD7D66-2D91-456C-AE4E-E2F6F5B8BCAD}\"" +
        Environment.NewLine;
      var expected =
        "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Shared\", \"Shared\", \"{8FCD7D66-2D91-456C-AE4E-E2F6F5B8BCAD}\"" +
        Environment.NewLine;

      Assert.Equal(expected, SlnAssetPostprocessor.ProcessSlnText(input));
    }

    // [Fact]
    public void TestFixesInvalidCSharpGuid()
    {
      // See https://youtrack.jetbrains.com/issue/RIDER-1261 (demo project shows type in C# guid: FA*A*04EC0-301F-11D3-BF4B-00C04F79EFBC)
      var input =
        "Project(\"{FAA04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Shared\", \"Shared\", \"{8FCD7D66-2D91-456C-AE4E-E2F6F5B8BCAD}\"" +
        Environment.NewLine;
      var expected =
        "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Shared\", \"Shared\", \"{8FCD7D66-2D91-456C-AE4E-E2F6F5B8BCAD}\"" +
        Environment.NewLine;
      
      Assert.Equal(expected, SlnAssetPostprocessor.ProcessSlnText(input));
    }

    // [Fact]
    public void TestCorectGuidNotModified()
    {
      var input = "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"BestHTTP\", \"BestHTTP.csproj\", \"{A311886C-D085-4914-A8E5-6DF7C92112D8}\""+Environment.NewLine;
      var expected = "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"BestHTTP\", \"BestHTTP.csproj\", \"{A311886C-D085-4914-A8E5-6DF7C92112D8}\""+Environment.NewLine;
      Assert.Equal(expected, SlnAssetPostprocessor.ProcessSlnText(input));
    }
  }
}