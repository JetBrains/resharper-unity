using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class GuidReferenceInfoAnalyzerTests : JsonNewHighlightingTestBase<GuidReferenceInfo>
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => @"AsmDef\Daemon\Stages\Analysis\GuidReference";

        [Test]
        public void Test01()
        {
            DoTestSolution(new[] { "GuidReference.asmdef" },
                new[] { "GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta" });
        }
    }
}
