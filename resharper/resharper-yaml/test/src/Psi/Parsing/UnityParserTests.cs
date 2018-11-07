using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Yaml.Tests.Psi.Parsing
{
  public class UnityParserTests : ParserTestBase<YamlLanguage>
  {
    protected override string RelativeTestDataPath => @"Psi\Parsing\Unity";

    [TestCase("FolderMeta")]
    [TestCase("PluginMeta")]
    [TestFileExtension(".meta")]
    public void TestUnityMetaFile(string name) => DoOneTest(name);

    // Unity's YAML is "special" aka invalid. It can include the keyword "stripped" after the node properties of
    // documents in a .unity file. I don't know what the keyword indicates, and I don't like special-casing the parser,
    // but I REALLY want to parse Unity YAML files...
    [TestCase("Gems-InvalidStripped")]
    [TestCase("StrippedExample")]
    [TestCase("SpaceShooter")]
    [TestFileExtension(".unity")]
    public void TestUnitySceneFile(string name) => DoOneTest(name);
  }
}