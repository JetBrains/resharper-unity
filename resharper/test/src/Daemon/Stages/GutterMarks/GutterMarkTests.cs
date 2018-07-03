using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Daemon.Stages.GutterMarks
{
    [TestUnity]
    public class GutterMarkTests : CSharpHighlightingTestBase<UnityGutterMarkInfo>
    {
        protected override string RelativeTestDataPath => @"Daemon\Stages\GutterMark";

        [Test] public void Test01() { DoNamedTest(); }
    }
}