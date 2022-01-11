using JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.Analysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Daemon.Stages.GutterMarks
{
    [TestUnity]
    public class GutterMarkTests : CSharpHighlightingTestBase<IUnityIndicatorHighlighting>
    {
        protected override string RelativeTestDataPath => @"CSharp\Daemon\Stages\GutterMark";

        // ********************************************************************
        // IMPORTANT! Keep in sync with equivalent class in Unity.Rider.Tests
        // ********************************************************************

        [Test] public void Test01() { DoNamedTest(); }

        [Test, TestUnity(UnityVersion.Unity2019_4)] public void TestGenericSerialisedFields_2019_4() { DoNamedTest2(); }
        [Test, TestUnity(UnityVersion.Unity2020_1)] public void TestGenericSerialisedFields_2020_1() { DoNamedTest2(); }
    }
}