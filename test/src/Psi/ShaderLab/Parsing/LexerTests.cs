using System;
using System.IO;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Text;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Psi.ShaderLab.Parsing
{
    [TestUnity]
    [TestFileExtension(ShaderLabProjectFileType.SHADER_EXTENSION)]
    public class LexerTests : LexerTestBase
    {
        protected override string RelativeTestDataPath => @"psi\shaderLab\lexing";

        protected override ILexer CreateLexer(StreamReader sr)
        {
            return new ShaderLabLexerGenerated(new StringBuffer(sr.ReadToEnd()));
        }

        protected override void WriteToken(TextWriter writer, ILexer lexer)
        {
            string str1 = lexer.GetCurrTokenText().Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
            string str2 = string.Format("{0:D4}: {1} '{2}'", lexer.TokenStart, lexer.TokenType, str1);
            writer.WriteLine(str2);
            Console.WriteLine(str2);
        }

        [TestCase("Everything")]
        [TestCase("Properties")]
        [TestCase("PropertyAttributes")]
        [TestCase("Tags")]
        [TestCase("MultipleTagsOnSingleLine")]
        [TestCase("CgInclude")]
        [TestCase("CgProgram")]
        [TestCase("UnterminatedString")]
        [TestCase("UnterminatedComment")]
        [TestCase("UnterminatedCgInclude")]
        [TestCase("UnterminatedCgProgram")]
        public void TestLexer(string name) => DoOneTest(name);
    }
}