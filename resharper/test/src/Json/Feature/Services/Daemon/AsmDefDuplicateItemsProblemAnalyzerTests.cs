using JetBrains.ReSharper.Daemon.JavaScript.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.FeaturesTestFramework.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefDuplicateItemsProblemAnalyzerTests : HighlightingTestBase
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonLanguage.Instance;

        protected override string RelativeTestDataPath => @"Json\Daemon\Stages\Analysis\";

        protected override bool HighlightingPredicate(IHighlighting highlighting, IPsiSourceFile sourceFile)
        {
            return highlighting is JsonValidationFailedWarning;
        }

        // TODO: ReSharper will run element problem analyzers twice for JSON files
        // Which means we get multiple highlights. Not a huge deal in practice, but if this
        // test suddenly starts to fail, that might be why. See RSRP-467138
        [Test] public void Test01() { DoNamedTest(); }
    }
}