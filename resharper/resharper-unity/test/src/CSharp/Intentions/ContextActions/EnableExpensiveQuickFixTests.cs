using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class EnableExpensiveAvailabilityTests : ContextActionAvailabilityAfterSwaTestBase<AddExpensiveMethodAttributeContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;

        protected override string ExtraPath => @"EnableExpensive\Availability";
        
        [Test] public void Everything() { DoNamedTest(); }
    }

    [TestUnity]
    public class EnableExpensiveAttributeContextActionsTests : ContextActionExecuteAfterSwaTestBase<AddExpensiveMethodAttributeContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;

        protected override string ExtraPath => "EnableExpensive";
        
        [Test] public void TransitiveAction() { DoNamedTest(); }
    
        [Test] public void SuppressMessageAction() { DoNamedTest(); }
    }
}