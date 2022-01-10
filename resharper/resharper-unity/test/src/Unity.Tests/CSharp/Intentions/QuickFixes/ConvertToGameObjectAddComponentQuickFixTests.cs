using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    [TestUnity]
    public class ConvertToGameObjectAddComponentQuickFixAvailabilityTests
        : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertToGameObjectAddComponent\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    public class ConvertToGameObjectAddComponentQuickFixTests
        : QuickFixTestBase<ConvertToGameObjectAddComponentQuickFix>
    {
        protected override string RelativeTestDataPath => @"CSharp\Intentions\QuickFixes\ConvertToGameObjectAddComponent";
        protected override bool AllowHighlightingOverlap => true;

        [Test] public void Test01() { DoNamedTest(); }
    }
}