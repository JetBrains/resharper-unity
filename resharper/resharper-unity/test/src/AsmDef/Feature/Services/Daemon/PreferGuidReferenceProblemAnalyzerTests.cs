using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class PreferGuidReferenceProblemAnalyzerTests : JsonNewHighlightingTestBase<PreferGuidReferenceWarning>
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => @"AsmDef\Daemon\Stages\Analysis\PreferGuidReference";

        [Test] public void TestShowHint() { DoNamedTest2("Ref1.asmdef"); }
        [Test] public void TestNoHintOnUnresolvedReference() { DoNamedTest2(); }
    }
}
