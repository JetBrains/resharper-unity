using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Yaml.Psi.Parsing
{
  [TestFileExtension(".unity")]
  public class ParserTests : TestFramework.ParserTestBase<YamlLanguage>
  {
    protected override string RelativeTestDataPath => @"Yaml\Psi\Parsing";

    [TestCase("Scene")]
    public void TestParser(string name) => DoOneTest(name);

    [TestCase("TagManager")]
    [TestFileExtension(".asset")]
    public void TestUnityTagManager(string name) => DoOneTest(name);
  }
}