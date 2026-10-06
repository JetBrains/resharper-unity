using JetBrains.ReSharper.Plugins.Tests.TestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class BurstDiscardAvailabilityTests : ContextActionAvailabilityAfterSwaTestBase<AddDiscardAttributeContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"BurstDiscardAttribute\Availability";

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst quick fixes", "Add BurstDiscard attribute")] public void Everything() { DoNamedTest(); }
    }

    [TestUnity]
    public class BurstDiscardContextActionTests : ContextActionExecuteAfterSwaTestBase<AddDiscardAttributeContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "BurstDiscardAttribute";

        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst quick fixes", "Add BurstDiscard attribute")] public void TransitiveActions1() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst quick fixes", "Add BurstDiscard attribute")] public void TransitiveActions2() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("DOTS and Burst", "Burst quick fixes", "Add BurstDiscard attribute")] public void TransitiveActions3() { DoNamedTest(); }
    }
}