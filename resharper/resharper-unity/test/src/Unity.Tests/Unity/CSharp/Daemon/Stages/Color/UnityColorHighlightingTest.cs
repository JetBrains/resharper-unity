using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Color
{
    [Category("ColorHighlighting")]
    [TestUnity]
    public class UnityColorHighlightingTest : CSharpHighlightingTestBase<ColorHighlighting>
    {
        protected override bool ColorIdentifiers => true;
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\Colors";

        [Test] public void TestColor() { DoNamedTest2(); }
        [Test] public void TestColor32() { DoNamedTest2(); }
    }
}