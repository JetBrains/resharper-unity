using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Json.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencingSelfProblemAnalyzerTests : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonLanguage.Instance;
        protected override string RelativeTestDataPath => @"Json\Daemon\Stages\Analysis\ReferencingSelf";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is ReferencingSelfError;
        }

        [Test] public void Test01() { DoNamedTest(); }
    }
}