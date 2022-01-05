using System.IO;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Text;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Json.Psi.Parsing
{
    [TestFileExtension(JsonProjectFileType.JSON_EXTENSION)]
    public class LexerTests : LexerTestBase
    {
        protected override string RelativeTestDataPath => @"Json\Psi\Lexing";

        protected override ILexer CreateLexer(IBuffer buffer)
        {
            var text = buffer.GetText();
            text = NormaliseLindEndings(text);
            return new JsonNewLexerGenerated(new StringBuffer(text));
        }

        private string NormaliseLindEndings(string text)
        {
            // TeamCity doesn't respect .gitattributes and pulls everything out as
            // LF, instead of CRLF. Normalise to CRLF
            return !text.Contains("\r\n") ? text.Replace("\n", "\r\n") : text;
        }

        protected override void WriteToken(TextWriter writer, ILexer lexer)
        {
            string str1 = lexer.GetTokenText().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            string str2 = string.Format("{0:D4}: {1} '{2}'", lexer.TokenStart, lexer.TokenType, str1);
            writer.WriteLine(str2);
//            Console.WriteLine(str2);
        }

        [TestCase("Everything")]
        [TestCase("empty")]
        [TestCase("emptyArray")]
        [TestCase("emptyObject")]
        [TestCase("boolLiteral")]
        [TestCase("stringLiteral")]
        [TestCase("literal")]
        [TestCase("array")]
        [TestCase("object")]
        [TestCase("object1")]
        [TestCase("object2")]
        [TestCase("object3")]
        [TestCase("object4")]
        [TestCase("object5")]
        [TestCase("object6")]
        public void TestLexer(string name) => DoOneTest(name);
    }
}