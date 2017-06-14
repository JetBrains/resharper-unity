using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon
{
    public class SyntaxHighlightingTest : ShaderLabHighlightingTestBase
    {
        protected override string RelativeTestDataPath => @"daemon\SyntaxHighlighting";

        [Test] public void TestSyntax01() { DoNamedTest2(); }
        [Test] public void TestSyntax02() { DoNamedTest2(); }
        [Test] public void TestSyntax03() { DoNamedTest2(); }
    }
}