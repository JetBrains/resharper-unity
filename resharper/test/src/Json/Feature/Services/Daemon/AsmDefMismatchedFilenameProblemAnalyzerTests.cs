using JetBrains.ReSharper.Plugins.Unity.Json.Daemon.Errors;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Feature.Services.Daemon
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefMismatchedFilenameProblemAnalyzerTests : JsonHighlightingTestBase<MismatchedAsmDefFilenameWarning>
    {
        protected override string RelativeTestDataPath => @"Json\Daemon\Stages\Analysis\MismatchedFilename";

        [Test] public void Test01() { DoNamedTest(); }
    }
}