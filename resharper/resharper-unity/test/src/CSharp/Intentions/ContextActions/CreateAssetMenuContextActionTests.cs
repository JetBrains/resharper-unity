using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.ContextActions
{
    [TestUnity]
    public class CreateAssetMenuContextActionAvailabilityTests
        : ContextActionAvailabilityTestBase<CreateAssetMenuContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => @"CreateAssetMenu";

        [Test] public void TestAvailability01() { DoNamedTest2(); }
    }

    [TestUnity]
    public class CreateAssetMenuContextActionTests : ContextActionExecuteTestBase<CreateAssetMenuContextAction>
    {
        protected override string RelativeTestDataPath => @"CSharp\" + base.RelativeTestDataPath;
        protected override string ExtraPath => "CreateAssetMenu";

        [Test] public void AddCreateAssetMenu() { DoNamedTest(); }
    }
}