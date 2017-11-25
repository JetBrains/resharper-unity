using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.ContextActions
{
    [TestUnity]
    public class MarkSerializeFieldContextActionAvailabilityTest
        : ContextActionAvailabilityTestBase<MarkSerializeFieldAction>
    {
        protected override string ExtraPath => @"MarkSerializeField";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class MarkSerializeFieldContextActionExecutionTest
        : ContextActionExecuteTestBase<MarkSerializeFieldAction>
    {
        protected override string ExtraPath => "MarkSerializeField";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }
}