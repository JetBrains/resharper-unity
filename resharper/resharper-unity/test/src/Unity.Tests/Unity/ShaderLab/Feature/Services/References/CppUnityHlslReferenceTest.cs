using System.Collections;
using System.Linq;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Cpp.Search;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Cpp.Language;
using JetBrains.ReSharper.Psi.Cpp.Resolve;
using JetBrains.ReSharper.Psi.Cpp.Symbols;
using JetBrains.ReSharper.Psi.Cpp.Tree;
using JetBrains.ReSharper.Psi.Cpp.Types;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.ShaderLab.Feature.Services.References
{
    [TestUnity, RequireHlslSupport, Category("Cpp.Unity")]
    [CgIncludesDirectory("ShaderLab/CGIncludes")]
    class CppUnityHlslReferenceTest : ReferenceTestBase, IEnumerable
    {
        protected override string RelativeTestDataPath => @"ShaderLab\References";

        [TestCase("Test01.shader")]
        [TestCase("Test02.compute")]
        [TestCase("Test03.hlsl")]
        public void Test(string filename) => DoTestSolution(filename);

        IEnumerator IEnumerable.GetEnumerator() { return TestDataPath
            .GetDirectoryEntries("*.hlsl", true)
            .Select(x => new TestCaseData(x.Name).SetName(x.MakeRelativeTo(TestDataPath).ToString())).GetEnumerator(); }

        protected override bool AcceptReference(IReference reference)
        {
            if (reference is CppQualifiedReferenceSimpleReference<BaseQualifiedReference> qualifiedRef)
                return qualifiedRef.GetTreeNode() is not DeclarationSpecifierTypename;
            return reference is not (CppDirectiveReference or CppOverrideOrFinalSpecifierReference);
        }

        protected override string Format(IDeclaredElement? declaredElement, ISubstitution substitution, PsiLanguageType languageType, DeclaredElementPresenterStyle presenter, IProject testProject, IReference reference)
        {
            if (declaredElement == null)
                return @"null";

            if (declaredElement is CppResolveEntityDeclaredElement cppElement)
            {
                var entity = cppElement.GetResolveEntity();
                var name = ResolveEntityPrettyPrinter.GetFullName(entity);
                var type = cppElement.GetElementType();
                return $"Name:{name}, type:{type}{BuildExtraInfo(entity)}";
            }

            if (declaredElement is CppPreprocessorDeclaredElement ppElement)
                return $"Name:{ppElement.ShortName}, macro:{ppElement.GetPPSymbol().Location.TextOffset}";

            if (declaredElement is CppPathDeclaredElement pathElement)
                return $"Files:{string.Concat(pathElement.GetSourceFiles().Select(x => x.Name).ToArray())}";

            if (declaredElement is CppModuleDeclaredElement moduleElement)
                return $"Module:{moduleElement.ShortName}";

            return "Unknown DeclaredElement";
        }

        private static string BuildExtraInfo(ICppResolveEntity ent)
        {
            var res = "";
            var type = ent switch
            {
                AbstractBuiltinResolveEntity resolveEntity => resolveEntity.GetCppType(),
                ICppDeclaratorResolveEntity declEnt => declEnt.GetCppType(),
                ICppFunctionTemplateDeclaratorResolveEntity templateDecl => templateDecl.PrimaryTemplateDeclaration.GetCppType(),
                _ => CppQualType.NullType()
            };
            if (!type.IsNullType() && type.IsFunctionType())
            {
                res = ", cpp-type: " + type.DbgDescription;
                if (ent is ICppDeclaratorResolveEntity declEnt && declEnt.DeclarationSpecifiers != CppDeclarationSpecifiers.None)
                    res += ", specs: " + declEnt.DeclarationSpecifiers.SpecsToString();
            }

            return res;
        }
    }
}