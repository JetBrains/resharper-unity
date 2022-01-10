using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class PerformanceAnalysisDisableAvailabilityTests : ContextActionAvailabilityAfterSwaTestBase<AddPerformanceAnalysisDisableCommentContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"PerformanceDisableByComment\Availability";

        [Test] public void Everything() { DoNamedTest(); }
    }

    [TestUnity]
    public class PerformanceAnalysisDisableContextActionTests : ContextActionExecuteAfterSwaTestBase<AddPerformanceAnalysisDisableCommentContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "PerformanceDisableByComment";

        [Test] public void TestSimple1() { DoNamedTest(); }
        [Test] public void TestSimple2() { DoNamedTest(); }
        [Test] public void TestSimple3() { DoNamedTest(); }
    }
}