using System;
using System.IO;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Text;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Psi.Parsing
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class LexerTests : LexerTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\Psi\Lexing";

        protected override ILexer CreateLexer(IBuffer buffer)
        {
            var text = buffer.GetText();
            text = NormaliseLindEndings(text);
            return new ShaderLabLexer(new StringBuffer(text));
        }

        private string NormaliseLindEndings(string text)
        {
            // TeamCity doesn't respect .gitattributes and pulls everything out as
            // LF, instead of CRLF. Normalise to CRLF
            return !text.Contains("\r\n") ? text.Replace("\n", "\r\n") : text;
        }

        protected override void WriteToken(TextWriter writer, ILexer lexer)
        {
            string str1 = lexer.GetCurrTokenText().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            string str2 = string.Format("{0:D4}: {1} '{2}'", lexer.TokenStart, lexer.TokenType, str1);
            writer.WriteLine(str2);
            Console.WriteLine(str2);
        }

        [TestCase("Everything")]
        [TestCase("Preprocessor")]
        [TestCase("Properties")]
        [TestCase("PropertyAttributes")]
        [TestCase("Tags")]
        [TestCase("MultipleTagsOnSingleLine")]
        [TestCase("CgInclude")]
        [TestCase("CgProgram")]
        [TestCase("HlslInclude")]
        [TestCase("HlslProgram")]
        [TestCase("GlslInclude")]
        [TestCase("GlslProgram")]
        [TestCase("UnterminatedString")]
        [TestCase("UnterminatedComment")]
        [TestCase("UnterminatedComment2")]
        [TestCase("UnterminatedCgInclude")]
        [TestCase("UnterminatedCgProgram")]
        [TestCase("UnterminatedHlslInclude")]
        [TestCase("UnterminatedHlslProgram")]
        [TestCase("UnterminatedGlslInclude")]
        [TestCase("UnterminatedGlslProgram")]
        [TestCase("MultilineComment")]
        [TestCase("MismatchedMultilineComment")]
        [TestCase("Float")]
        public void TestLexer(string name) => DoOneTest(name);
    }
}