using System;
using JetBrains.Rider.Unity.Editor.AssetPostprocessors;
using NUnit.Framework;

namespace JetBrains.Rider.Unity.Editor.Tests
{
  [TestFixture]
  public class SlnAssetPostprocessorTest
  {
    [Test]
    public void Test()
    {
      var folder = "Project(\"{2150E333-8FDC-42A3-9474-1A3956D46DE8}\") = \"Shared\", \"Shared\", \"{8FCD7D66-2D91-456C-AE4E-E2F6F5B8BCAD}\""+Environment.NewLine;
      var exFold = "Project(\"{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1}\") = \"Shared\", \"Shared\", \"{8FCD7D66-2D91-456C-AE4E-E2F6F5B8BCAD}\""+Environment.NewLine;
      var result = SlnAssetPostprocessor.ProcessSlnText(folder);
      
      Assert.AreEqual(exFold, result);

      var t =        "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"BestHTTP\", \"BestHTTP.csproj\", \"{A311886C-D085-4914-A8E5-6DF7C92112D8}\""+Environment.NewLine;
      var expected = "Project(\"{E097FAD1-6243-4DAD-9C02-E9B9EFC3FFC1}\") = \"BestHTTP\", \"BestHTTP.csproj\", \"{A311886C-D085-4914-A8E5-6DF7C92112D8}\""+Environment.NewLine;
      Assert.AreEqual(expected, SlnAssetPostprocessor.ProcessSlnText(t));
    }
  }
}