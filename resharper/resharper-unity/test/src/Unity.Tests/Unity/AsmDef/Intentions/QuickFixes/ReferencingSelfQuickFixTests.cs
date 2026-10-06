using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class ReferencingSelfQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\ReferencingSelf\Availability";

        [Test, UnityPluginBackendChecklist("Assembly definition files", "Inspections", "Referencing self")] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class ReferencingSelfQuickFixTests : QuickFixTestBase<RemoveInvalidArrayItemQuickFix>
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\ReferencingSelf";

        [Test, UnityPluginBackendChecklist("Assembly definition files", "Inspections", "Referencing self")] public void Test01() { DoNamedTest(); }
        [Test, UnityPluginBackendChecklist("Assembly definition files", "Inspections", "Referencing self")] public void Test02() { DoNamedTest(); }
    }
}
