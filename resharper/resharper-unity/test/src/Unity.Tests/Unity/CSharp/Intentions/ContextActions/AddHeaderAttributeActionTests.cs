using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class AddHeaderAttributeActionAvailabilityTest
        : ContextActionAvailabilityTestBase<AddHeaderAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AddHeaderAttribute";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class AddHeaderAttributeActionExecutionTest
        : ContextActionExecuteTestBase<AddHeaderAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "AddHeaderAttribute";

        [Test] public void TestAddAttribute() { DoNamedTest2(); }
        [Test] public void TestAddToExistingAttributes() { DoNamedTest2(); }
        [Test] public void TestAddAttributeToAllFields() { DoNamedTest2(); }
        [Test] public void TestAddToOneOfMultipleFields() { DoNamedTest2(); }
    }
}