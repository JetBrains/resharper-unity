using JetBrains.ReSharper.Plugins.Unity.Cg.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Cg.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Cg.Psi.Parsing
{
    [TestUnity]
    [TestFileExtension(CgProjectFileType.CG_EXTENSION)]
    public class CgParserTests : ParserTestBase<CgLanguage>
    {
        protected override string RelativeTestDataPath => @"Cg\Psi\Parsing";
        
        [TestCase("AllDeclarations")]
        
        [TestCase("Assignments")]
        
        [TestCase("Comment")]
        
        [TestCase("Empty")]
        
        [TestCase("FieldDeclaration")]
        [TestCase("FieldDeclarationNoSemicolon")]
        [TestCase("FieldDeclarationScalarTypes")]
        [TestCase("FieldDeclarationReferencedType")]
        [TestCase("FieldDeclarationWithInitializer")]
        
        [TestCase("FunctionArguments")]
        [TestCase("FunctionOneArgument")]
        [TestCase("FunctionTwoArguments")]
        
        [TestCase("FunctionCall")]
        
        [TestCase("FunctionBuiltInReturnType")]
        [TestCase("FunctionVoidReturnType")]
        
        [TestCase("IfDirective")]
        [TestCase("IfdefDirective")]
        [TestCase("IfndefDirective")]
        [TestCase("NonConditionalDirectives")]
        
        [TestCase("LocalVariableDeclarations")]
        
        [TestCase("Semantics")]
        
        [TestCase("StructDeclaration")]
        [TestCase("StructDeclarationNoSemicolon")]
        [TestCase("StructDeclarationReferencedType")]
        
        [TestCase("UnaryOperator")]
        public void TestParser(string name) => DoOneTest(name);
    }
}