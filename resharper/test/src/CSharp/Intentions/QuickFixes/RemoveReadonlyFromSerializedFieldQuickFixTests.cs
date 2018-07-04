using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class RemoveReadonlyFromSerializedFieldQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RemoveReadonlyFromSerializedField\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }

    [TestUnity]
    public class RemoveReadonlyFromSerializedFieldQuickFixTests : CSharpQuickFixTestBase<RemoveReadonlyFromSerializedFieldQuickFix>
    {
        protected override string RelativeTestDataPath=> @"CSharp\Intentions\QuickFixes\RemoveReadonlyFromSerializedField";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
        [Test] public void Test03() { DoNamedTest(); }
    }
}