using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class PackageVersionInlayHintHighlightingTests : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => @"AsmDef\Daemon\Stages\InlayHints";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
                                                      IContextBoundSettingsStore settingsStore)
        {
            return highlighting is IAsmDefInlayHintHighlighting or IAsmDefInlayHintContextActionHighlighting;
        }

        // TODO: Add some kind of dummy package for testing
        [Test] public void TestPackageVersionInlayHints01() { DoTestSolution("PackageVersion.asmdef"); }
    }
}