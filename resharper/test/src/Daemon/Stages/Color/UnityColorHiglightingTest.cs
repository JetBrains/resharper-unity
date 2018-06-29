using JetBrains.ReSharper.Daemon.VisualElements;
using JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Color
{
    [Category("ColorHighlighting")]
    [TestUnity]
    public class UnityColorHiglightingTest : CSharpHighlightingTestBase<ColorHighlighting>
    {
        protected override bool ColorIdentifiers => true;
        protected override string RelativeTestDataPath => @"daemon\Stages\Colors";

        [Test] public void TestColor() { DoNamedTest2(); }
        [Test] public void TestColor32() { DoNamedTest2(); }
    }
}