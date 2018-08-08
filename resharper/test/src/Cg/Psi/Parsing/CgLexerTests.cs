using System;
using System.IO;
using JetBrains.ReSharper.Plugins.Unity.Cg.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi.Parsing;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.TestFramework;
using JetBrains.Text;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Cg.Psi.Parsing
{
    [TestUnity]
    [TestFileExtension(CgProjectFileType.CG_EXTENSION)]
    public class CgLexerTests : LexerTestBase
    {
        protected override string RelativeTestDataPath => @"Cg\Psi\Lexing";

        protected override ILexer CreateLexer(IBuffer buffer)
        {
            var text = buffer.GetText();
            text = NormaliseLindEndings(text);
            return new CgLexerGenerated(new StringBuffer(text));
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

        [TestCase("AsmStatement")]
        
        [TestCase("Basic_00")]
        
        [TestCase("Comment_00")]
        [TestCase("Comment_01")]
        [TestCase("Comment_02")]
        [TestCase("CommentSingleLineContinued")]
        
        [TestCase("Directive_00")]
        [TestCase("Directive_01")]
        [TestCase("Directive_02")]
        [TestCase("Directive_03")]
        
        [TestCase("Field_00")]
        
        [TestCase("Function_00")]
        
        [TestCase("Identifier_00")]
        
        [TestCase("NumericLiteral_00")]
        
        [TestCase("Operator_00")]
        
        [TestCase("Struct_00")]
        
        [TestCase("VariableDeclarationModifiers")]
        public void TestLexer(string name) => DoOneTest(name);
    }
}