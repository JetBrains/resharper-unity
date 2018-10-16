using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class AutoPropertyToSerializedBackingFieldAvailabilityTest
        : ContextActionAvailabilityTestBase<AutoPropertyToSerializedBackingFieldAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"AutoPropertyToSerializedBackingField";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
        [Test] public void TestAvailability02() { DoNamedTest2(); }
    }

    [TestUnity]
    public class AutoPropertyToSerializedBackingFieldExecutionTest
        : ContextActionExecuteTestBase<AutoPropertyToSerializedBackingFieldAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "AutoPropertyToSerializedBackingField";

        [Test] public void Test01() { DoNamedTest(); }
    }
}