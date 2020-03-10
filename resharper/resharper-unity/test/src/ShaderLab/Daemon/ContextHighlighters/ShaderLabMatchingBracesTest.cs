using JetBrains.ReSharper.FeaturesTestFramework.ContextHighlighters;
using JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.ShaderLab.Daemon.ContextHighlighters
{
    [TestFileExtension(ShaderLabProjectFileType.SHADERLAB_EXTENSION)]
    public class ShaderLabMatchingBracesTest : ContextHighlighterTestBase
    {
        protected override string RelativeTestDataPath => @"ShaderLab\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"Braces";

        [Test] public void TestBraces01() { DoNamedTest2(); }
        [Test] public void TestBraces02() { DoNamedTest2(); }

        [Test] public void TestBracks01() { DoNamedTest2(); }
        [Test] public void TestBracks02() { DoNamedTest2(); }

        [Test] public void TestParens01() { DoNamedTest2(); }
        [Test] public void TestParens02() { DoNamedTest2(); }

        [Test] public void TestQuotes01() { DoNamedTest2(); }
        [Test] public void TestQuotes02() { DoNamedTest2(); }

        [Test] public void TestCg01() { DoNamedTest2(); }
        [Test] public void TestCg02() { DoNamedTest2(); }
    }
}