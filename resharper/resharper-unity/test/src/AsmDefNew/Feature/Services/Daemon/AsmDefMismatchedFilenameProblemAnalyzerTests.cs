using JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.JsonNew.Psi;
using JetBrains.ReSharper.Plugins.Unity.Tests.AsmDefCommon.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDefNew.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefMismatchedFilenameProblemAnalyzerTests : JsonHighlightingTestBase<MismatchedAsmDefFilenameWarning>
    {
        protected override PsiLanguageType CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => $@"AsmDef\Daemon\Stages\Analysis\MismatchedFilename\{Utils.ProductGoldSuffix}";

        [Test] public void Test01() { DoNamedTest(); }
    }
}