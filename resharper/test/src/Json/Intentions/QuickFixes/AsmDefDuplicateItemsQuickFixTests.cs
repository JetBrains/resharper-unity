using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefDuplicateItemsQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"Json\Intentions\QuickFixes\DuplicateItems\Availability";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefDuplicateItemsQuickFixTests : QuickFixTestBase<AsmDefRemoveDuplicateItemQuickFix>
    {
        protected override string RelativeTestDataPath => @"Json\Intentions\QuickFixes\DuplicateItems";

        [Test] public void Test01() { DoNamedTest(); }
        [Test] public void Test02() { DoNamedTest(); }
    }
}