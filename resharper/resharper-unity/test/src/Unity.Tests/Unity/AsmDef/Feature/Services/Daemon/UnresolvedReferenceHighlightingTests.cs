using JetBrains.ReSharper.Plugins.Json.Psi;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class UnresolvedReferenceHighlightingTests : JsonNewHighlightingTestBase<UnresolvedProjectReferenceWarning>
    {
        protected override PsiLanguageType? CompilerIdsLanguage => JsonNewLanguage.Instance;
        protected override string RelativeTestDataPath => @"AsmDef\Daemon\Stages\Resolve\UnresolvedProject";

        [Test] public void Test01() { DoNamedTest(); }
    }
}
