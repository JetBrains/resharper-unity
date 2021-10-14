using JetBrains.Application.Settings;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class VersionDefinesInfoAnalyzerTests : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => @"AsmDef\Daemon\Stages\Analysis\VersionDefinesInfo";

        protected override string ProjectName => "Assembly-CSharp";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile,
                                                      IContextBoundSettingsStore settingsStore)
        {
            return highlighting is PackageNotInstalledInfo || highlighting is UnmetVersionConstraintInfo;
        }

        // TODO: Add packages for testing
        [Test] public void Test01() { DoNamedTest(); }
    }
}