using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.Psi;
using JetBrains.ReSharper.Plugins.Unity.Shaders.Cg.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.Cg.Psi.Parsing
{
    [RequireHlslSupport]
    [TestUnity]
    [TestFileExtension(CgProjectFileType.GLSL_EXTENSION)]
    public class CgParserTests : ParserTestBase<CgLanguage>
    {
        protected override string RelativeTestDataPath => @"Cg\Psi\Parsing";

        [TestCase("AllDeclarations")]

        [TestCase("AsmStatement")]

        [TestCase("Assignments")]

        [TestCase("BinaryOperator")]

        [TestCase("CommaExpression")]

        [TestCase("Comment")]

        [TestCase("DirectiveWithSpace")]

        [TestCase("DoStatement")]

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

        [TestCase("FunctionDeclarationAttribute")]

        [TestCase("FunctionArguments")]
        [TestCase("FunctionArgumentWithNumbers")]
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

        [TestCase("SwitchStatement")]

        [TestCase("UnaryOperator")]

        [TestCase("VariableDeclarationModifiers")]
        [TestCase("VariableDeclarationBufferType")]

        [TestCase("WhileStatement")]
        public void TestParser(string name) => DoOneTest(name);
    }
}
