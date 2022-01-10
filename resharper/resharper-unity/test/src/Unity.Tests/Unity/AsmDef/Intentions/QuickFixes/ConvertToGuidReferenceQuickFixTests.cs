using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class ConvertToGuidReferenceQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\ConvertToGuidReference\Availability";

        [Test] public void Test01() { DoNamedTest("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class ConvertToGuidReferenceQuickFixTests : QuickFixTestBase<ConvertToGuidReferenceQuickFix>
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\ConvertToGuidReference";

        [Test] public void Test01() { DoNamedTest("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
        [Test, ExecuteScopedQuickFixInFile] public void TestExecuteInScope() { DoNamedTest("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
    }
}
