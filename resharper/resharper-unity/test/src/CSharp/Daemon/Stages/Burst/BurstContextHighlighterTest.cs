using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Burst
{
    [TestUnity]
    [TestFileExtension(".cs")]
    public class BurstContextHighlighterTest : ContextHighlighterAfterSweaTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\BurstCodeAnalysis\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "";
        
        [Test] public void TestTransitive1() { DoNamedTest2(); }
        [Test] public void TestTransitive2() { DoNamedTest2(); }
        [Test] public void TestTransitive3() { DoNamedTest2(); }
        [Test] public void TestTransitive4() { DoNamedTest2(); }
        [Test] public void TestTransitive5() { DoNamedTest2(); }
        [Test] public void TestTransitive6() { DoNamedTest2(); }
        [Test] public void TestScopeBraces1() { DoNamedTest2(); }
        [Test] public void TestScopeBraces2() { DoNamedTest2(); }
        [Test] public void TestScopeBraces3() { DoNamedTest2(); }
        [Test] public void TestScopeBraces4() { DoNamedTest2(); }
        [Test] public void TestScopeBraces5() { DoNamedTest2(); }
        [Test] public void TestPerformanceCross1() { DoNamedTest2(); }
        [Test] public void TestPerformanceCross2() { DoNamedTest2(); }
    }
}