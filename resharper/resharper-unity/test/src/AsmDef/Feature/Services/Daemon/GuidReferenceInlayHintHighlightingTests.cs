using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class GuidReferenceInlayHintHighlightingTests : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => @"AsmDef\Daemon\Stages\InlayHints";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
                                                      IContextBoundSettingsStore settingsStore)
        {
            return highlighting is IAsmDefInlayHintHighlighting or IAsmDefInlayHintContextActionHighlighting;
        }

        [Test]
        public void TestGuidReferencesInlayHints01()
        {
            DoTestSolution(new[] { "GuidReference.asmdef" },
                new[] { "GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta" });
        }
    }
}