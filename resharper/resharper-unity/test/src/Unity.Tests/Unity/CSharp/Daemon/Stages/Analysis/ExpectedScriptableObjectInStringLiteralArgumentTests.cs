using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis
{
    [TestUnity]
    public class ExpectedScriptableObjectInStringLiteralArgumentTests
        : CSharpHighlightingTestBase<ExpectedScriptableObjectWarning>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Analysis";

        [Test]
        [UnityPluginBackendChecklist("C# code analysis", "String literal arguments", "Expected ScriptableObject type")]
        public void TestExpectedScriptableObjectInStringLiteral() { DoNamedTest2(); }
    }
}