using JetBrains.ReSharper.Plugins.Unity.Json.Daemon.Errors;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.JavaScript.LanguageImpl.JSon;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencingSelfProblemAnalyzerTests : JsonHighlightingTestBase<ReferencingSelfError>
    {
        protected override string RelativeTestDataPath => @"Json\Daemon\Stages\Analysis\ReferencingSelf";

        [Test] public void Test01() { DoNamedTest(); }
    }
}