using System.Collections.Generic;
using System.Linq;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Util;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Yaml.Psi.Parsing
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

    [Ignore("@matt please fix")]
    [TestCase("BinarySerialization")]
    [TestFileExtension(".asset")]
    public void TestBinarySerialization(string name) => DoOneTest(name);

    [TestCase("TagManager")]
    [TestFileExtension(".asset")]
    public void TestUnityTagManager(string name) => DoOneTest(name);


    protected override CaretPositionsProcessor CreateCaretPositionProcessor(FileSystemPath temporaryDirectory)
    {
      return new DefaultCaretPositionsProcessor(temporaryDirectory);
    }

    public class DefaultCaretPositionsProcessor : CaretPositionsProcessor
    {
      public DefaultCaretPositionsProcessor(FileSystemPath temporaryDirectory)
        : base(temporaryDirectory)
      {
      }

      // Make sure we have the equivalent of `{caret}` at the start of the file, so we don't have to edit the binary file
      public override void Process(FileSystemPath basePath, IEnumerable<string> files)
      {
        base.Process(basePath, files);
        if (CaretPosition == null)
        {
          var first = files.First();
          var file = FileSystemPath.TryParse(first);
          if (file.IsEmpty)
            file = basePath.NotNull().Combine(first);
          file = GetProcessedFilePath(file);
          CaretPosition = new CaretPosition(file.ToVirtualFileSystemPath(), 0);
        }
      }
    }
  }
}