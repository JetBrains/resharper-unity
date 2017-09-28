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
        
        [TestCase("BinaryOperator")]
        
        [TestCase("Comment")]
        
        [TestCase("DirectiveWithSpace")]
        
        [TestCase("Empty")]
        [TestCase("EmptyDirective")]
        [TestCase("EmptyDirectiveOnly")]
        
        [TestCase("FieldDeclaration")]
        [TestCase("FieldDeclarationNoSemicolon")]
        [TestCase("FieldDeclarationScalarTypes")]
        [TestCase("FieldDeclarationReferencedType")]
        [TestCase("FieldDeclarationWithInitializer")]
        
        [TestCase("ForStatementEmptyHeader")]
        [TestCase("ForStatementNoBrackets")]
        [TestCase("ForStatementWithBrackets")]
        
        [TestCase("FunctionArguments")]
        [TestCase("FunctionOneArgument")]
        [TestCase("FunctionTwoArguments")]
        
	    [TestCase("FunctionBodyEmptyStatement")]        
	    [TestCase("FunctionBodyNestedBlock")]

	    [TestCase("FunctionCall_00")]
        [TestCase("FunctionCall_01")]
        
        [TestCase("FunctionBuiltInReturnType")]
        [TestCase("FunctionVoidReturnType")]
        
        [TestCase("IfDirective")]
        [TestCase("IfdefDirective")]
        [TestCase("IfndefDirective")]
        [TestCase("NonConditionalDirectives")]
        [TestCase("OneDirective")]
        
        [TestCase("IfStatementFullBrackets")]
        [TestCase("IfStatementMixedBrackets")]
        [TestCase("IfStatementNoBrackets")]
        [TestCase("IfStatementNoBracketsReturn")]
        
        [TestCase("LocalVariableDeclarations")]
        
        [TestCase("Semantics")]
        
        [TestCase("StructDeclaration")]
        [TestCase("StructDeclarationNoSemicolon")]
        [TestCase("StructDeclarationReferencedType")]
        
        [TestCase("UnaryOperator")]
        
        [TestCase("VariableDeclarationModifiers")]
        public void TestParser(string name) => DoOneTest(name);
    }
}
