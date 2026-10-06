using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class PreferGuidReferenceProblemAnalyzerTests : JsonNewHighlightingTestBase<PreferGuidReferenceWarning>
    {
        protected override PsiLanguageType? CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => @"AsmDef\Daemon\Stages\Analysis\PreferGuidReference";

        [Test, UnityPluginBackendChecklist("Assembly definition files", "GUID references", "Prefer GUID reference")] public void TestShowHint() { DoNamedTest2("Ref1.asmdef"); }
        [Test, UnityPluginBackendChecklist("Assembly definition files", "GUID references", "Prefer GUID reference")] public void TestNoHintOnUnresolvedReference() { DoNamedTest2(); }

        [Test, TestFileExtension(".asmref"), UnityPluginBackendChecklist("Assembly definition files", "GUID references", "Prefer GUID reference")] public void TestAsmRef() { DoNamedTest2("Ref1.asmdef"); }
    }
}
