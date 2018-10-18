using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencingSelfQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"Json\Intentions\QuickFixes\ReferencingSelf\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencingSelfQuickFixTests : QuickFixTestBase<AsmDefRemoveInvalidArrayItemQuickFix>
    {
        protected override string RelativeTestDataPath => @"Json\Intentions\QuickFixes\ReferencingSelf";

        [Test] public void Test01() { DoNamedTest(); }
    }
}