using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class ExpectedComponentInStringLiteralArgumentTests
        : CSharpHighlightingTestBase<ExpectedComponentWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test] public void TestExpectedComponentInStringLiteral() { DoNamedTest2(); }
        [Test] public void TestExpectedMonoBehaviourInStringLiteral() { DoNamedTest2(); }
    }
}