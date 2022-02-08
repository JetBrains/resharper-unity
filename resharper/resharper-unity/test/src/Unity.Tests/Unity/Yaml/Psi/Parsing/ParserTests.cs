using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Yaml.Psi.Parsing
{
  // TODO: Consider moving these tests, perhaps as example YAML files in resharper-yaml
  // The .unity and .asset file extensions are registered to the UnityYaml language, but there isn't a PSI language
  // service, so we don't get a lexer or parser from production code. We do all lexing/parsing explicitly and on-demand
  // when these files are indexed.
  // The UnityYamlProjectFileLanguageServiceTest component overrides the PSI language type for files registered by
  // UnityYamlProjectFileType. It returns YamlLanguage instead of UnityYamlLanguage. This means test code has
  // potentially different behaviour to production code.
  public class ParserTests : ParserTestBase<YamlLanguage>
  {
    protected override string RelativeTestDataPath => @"Yaml\Psi\Parsing";

    [TestCase("Scene")]
    [TestFileExtension(".unity")]
    public void TestParser(string name) => DoOneTest(name);

    [TestCase("TagManager")]
    [TestFileExtension(".asset")]
    public void TestUnityTagManager(string name) => DoOneTest(name);
  }
}