using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    
    [TestUnity]
    public class BurstDiscardAvailabilityTests : ContextActionAvailabilityAfterSwaTestBase<AddDiscardAttributeContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;

        protected override string ExtraPath => @"BurstDiscardAttribute\Availability";
        
        [Test] public void Everything() { DoNamedTest(); }
    }
    
    [TestUnity]
    public class BurstDiscardContextActionTests : ContextActionExecuteAfterSwaTestBase<AddDiscardAttributeContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;

        protected override string ExtraPath => "BurstDiscardAttribute";

        [Test] public void TransitiveActions1() { DoNamedTest(); }

        [Test] public void TransitiveActions2() { DoNamedTest(); }

        [Test] public void TransitiveActions3() { DoNamedTest(); }
    }
}