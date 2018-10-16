using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.Analysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Daemon.Stages.GutterMarks
{
    [TestUnity]
    public class GutterMarkTests : CSharpHighlightingTestBase<UnityGutterMarkInfo>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\GutterMark";

        [Test] public void Test01() { DoNamedTest(); }
    }
}