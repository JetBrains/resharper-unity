using System.IO;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Text;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Yaml.Psi.Parsing
{
  [TestFileExtension(TestYamlProjectFileType.YAML_EXTENSION)]
  public class UnityLexerTests : LexerTestBase
  {
    protected override string RelativeTestDataPath => @"Psi\Lexing\Unity";

    protected override ILexer CreateLexer(IBuffer buffer)
    {
      return new YamlLexerFactory().CreateLexer(buffer);
    }

    protected override void WriteToken(TextWriter writer, ILexer lexer)
    {
      var text = lexer.GetTokenText();

      var token = lexer.TokenType;
      if (token == YamlTokenType.NON_PRINTABLE)
      {
        text = $"{lexer.TokenStart:D4}: {lexer.TokenType} length: {lexer.TokenEnd - lexer.TokenStart}";
      }
      else
      {
        text = text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        text = $"{lexer.TokenStart:D4}: {lexer.TokenType} '{text}'";
      }
      writer.WriteLine(text);
      // Console.WriteLine(text);
    }

    // Some assets can be forced to serialise as binary, e.g. NavMesh.asset, LightingData.asset and anything with
    // the [PreferBinarySerialization] attribute. See https://docs.unity3d.com/ScriptReference/PreferBinarySerialization.html
    [TestCase("BinarySerialization")]
    [TestFileExtension(".asset")]

    public void TestLexer(string name) => DoOneTest(name);
  }
}
