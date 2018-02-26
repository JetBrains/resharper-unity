using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.ContextActions
{
    [TestUnity]
    public class MarkFieldNonSerializedActionAvailabilityTest
        : ContextActionAvailabilityTestBase<MarkFieldNonSerializedAction>
    {
        protected override string ExtraPath => @"MarkFieldNonSerialized";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class MarkFieldNonSerializedActionExecutionTest
        : ContextActionExecuteTestBase<MarkFieldNonSerializedAction>
    {
        protected override string ExtraPath => "MarkFieldNonSerialized";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
    }
}