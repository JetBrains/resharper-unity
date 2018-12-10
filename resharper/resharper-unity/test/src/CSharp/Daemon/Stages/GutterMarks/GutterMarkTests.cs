using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.GutterMarks
{
    [TestUnity]
    public class GutterMarkTests : CSharpHighlightingTestWithProductDependentGoldBase<IUnityHighlighting>
    {
        protected override string RelativeTestDataRoot => @"CSharp\Daemon\Stages\GutterMark";

        [Test] public void Test01() { DoNamedTest(); }
    }
}