using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class AddRangeAttributeActionAvailabilityTest
        : ContextActionAvailabilityTestBase<AddRangeAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AddRangeAttribute";

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Add Range attribute")] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class AddRangeAttributeActionExecutionTest
        : ContextActionExecuteTestBase<AddRangeAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "AddRangeAttribute";

        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Add Range attribute")] public void TestAddAttribute() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Add Range attribute")] public void TestAddToExistingAttributes() { DoNamedTest2(); }
        [Test, UnityPluginBackendChecklist("Context actions", "Serialized fields", "Add Range attribute")] public void TestAddToOneOfMultipleFields() { DoNamedTest2(); }
    }
}