using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon
{
    public class ShaderLabSyntaxErrorHighlightingTest : ShaderLabHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"daemon\SyntaxHighlighting";

        [Test] public void TestSyntax01() { DoNamedTest2(); }
        [Test] public void TestSyntax02() { DoNamedTest2(); }
        [Test] public void TestSyntax03() { DoNamedTest2(); }
    }
}