using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.AsmDef.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencingSelfQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\ReferencingSelf\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefReferencingSelfQuickFixTests : QuickFixTestBase<AsmDefRemoveInvalidArrayItemQuickFix>
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\ReferencingSelf";

        [Test] public void Test01() { DoNamedTest(); }
    }
}
