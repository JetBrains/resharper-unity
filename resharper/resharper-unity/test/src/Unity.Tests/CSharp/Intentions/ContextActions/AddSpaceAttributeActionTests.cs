using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class AddSpaceAttributeActionAvailabilityTest
        : ContextActionAvailabilityTestBase<AddSpaceAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AddSpaceAttribute";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class AddSpaceAttributeActionExecutionTest
        : ContextActionExecuteTestBase<AddSpaceAttributeAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "AddSpaceAttribute";

        [Test] public void TestAddAttribute() { DoNamedTest2(); }
        [Test] public void TestAddToExistingAttributes() { DoNamedTest2(); }
        [Test] public void TestAddAttributeToAllFields() { DoNamedTest2(); }
        [Test] public void TestAddToOneOfMultipleFields() { DoNamedTest2(); }
    }
}