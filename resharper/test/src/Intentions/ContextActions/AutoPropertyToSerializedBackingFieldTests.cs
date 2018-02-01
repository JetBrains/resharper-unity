using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.ContextActions
{
    [TestUnity]
    public class AutoPropertyToSerializedBackingFieldAvailabilityTest
        : ContextActionAvailabilityTestBase<AutoPropertyToSerializedBackingFieldAction>
    {
        protected override string ExtraPath => @"AutoPropertyToSerializedBackingField";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
        [Test] public void TestAvailability02() { DoNamedTest2(); }
    }

    [TestUnity]
    public class AutoPropertyToSerializedBackingFieldExecutionTest
        : ContextActionExecuteTestBase<AutoPropertyToSerializedBackingFieldAction>
    {
        protected override string ExtraPath => "AutoPropertyToSerializedBackingField";

        [CSharpLanguageLevel(CSharpLanguageLevel.CSharp50)]
        [Test] public void Test01() { DoNamedTest(); }
    }
}