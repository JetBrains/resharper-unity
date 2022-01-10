using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    [TestCompilationSymbols("RESHARPER_UNITY_TEST_DEFINED_SYMBOL")]
    public class DefineConstraintsInfoAnalyzerTests : JsonNewHighlightingTestBase<UnmetDefineConstraintInfo>
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => @"AsmDef\Daemon\Stages\Analysis\UnmetDefineConstraints";

        protected override string ProjectName => "Assembly-CSharp";

        [Test] public void Test01() { DoNamedTest(); }
    }
}