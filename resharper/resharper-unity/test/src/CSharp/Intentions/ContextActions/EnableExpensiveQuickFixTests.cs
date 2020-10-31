using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis.AddExpensiveComment;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class EnableExpensiveAvailabilityTests : ContextActionAvailabilityAfterSwaTestBase<AddExpensiveCommentContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;

        protected override string ExtraPath => @"EnableExpensive\Availability";
        
        [Test] public void Everything() { DoNamedTest(); }
    }

    [TestUnity]
    public class EnableExpensiveAttributeContextActionsTests : ContextActionExecuteAfterSwaTestBase<AddExpensiveCommentContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;

        protected override string ExtraPath => "EnableExpensive";
        
        [Test] public void AddComment() { DoNamedTest(); }
    }
}