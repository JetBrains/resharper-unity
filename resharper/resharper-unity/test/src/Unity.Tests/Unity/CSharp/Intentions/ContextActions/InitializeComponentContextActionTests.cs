using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class InitializeComponentContextActionAvailabilityTests
        : ContextActionAvailabilityTestBase<InitializeFieldComponentContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"InitializeFieldComponent";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class InitializeComponentContextActionTests : ContextActionExecuteTestBase<InitializeFieldComponentContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "InitializeFieldComponent";

        [Test] public void AddRequireComponent() { DoNamedTest(); }
        [Test] public void AddRequireComponent2() { DoNamedTest(); }
        [Test] public void InitializeInStart() { DoNamedTest(); }
        [Test] public void InitializeInAwake() { DoNamedTest(); }
    }
}