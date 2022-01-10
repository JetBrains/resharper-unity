using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class AddExpensiveCommentAvailabilityTests : ContextActionAvailabilityAfterSwaTestBase<AddExpensiveCommentContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AddExpensiveComment\Availability";
        [Test] public void Everything() { DoNamedTest(); }
    }
    
    [TestUnity]
    public class AddExpensiveCommentContextActionTests : ContextActionExecuteAfterSwaTestBase<AddExpensiveCommentContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "AddExpensiveComment";
        [Test] public void TestSimple() { DoNamedTest(); }
    }
}