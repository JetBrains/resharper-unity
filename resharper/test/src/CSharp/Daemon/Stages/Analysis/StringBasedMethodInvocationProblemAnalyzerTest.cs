using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class StringBasedMethodInvocationProblemAnalyzerTest : CSharpHighlightingTestBase<StringBasedMethodInvocationProblemWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis\StringBasedMethodInvocationProblem";

        [Test] public void SendMessageTest() { DoNamedTest(); }
        [Test] public void SendMessageUpwardsTest() { DoNamedTest(); }
        [Test] public void BroadcastMessageTest() { DoNamedTest(); }
        [Test] public void InvokeTest() { DoNamedTest(); }
        [Test] public void InvokeRepeatingTest() { DoNamedTest(); }
    }
}