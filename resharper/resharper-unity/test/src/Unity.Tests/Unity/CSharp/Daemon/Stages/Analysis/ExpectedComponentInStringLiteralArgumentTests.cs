using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class ExpectedComponentInStringLiteralArgumentTests
        : CSharpHighlightingTestBase<ExpectedComponentWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test, UnityPluginBackendChecklist("C# code analysis", "String literal arguments", "Expected component type")] public void TestExpectedComponentInStringLiteral() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("C# code analysis", "String literal arguments", "Expected component type")] public void TestExpectedMonoBehaviourInStringLiteral() { DoNamedTest2(); }
    }
}