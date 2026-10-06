using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Intentions.QuickFixes
{
    [TestUnity]
    public class ConvertToGuidReferenceQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\ConvertToGuidReference\Availability";

        [Test, TestFileExtension(".asmdef"), UnityPluginBackendChecklist("Assembly definition files", "GUID references", "Convert to GUID reference")] public void Test01() { DoNamedTest("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
        [Test, TestFileExtension(".asmref"), UnityPluginBackendChecklist("Assembly definition files", "GUID references", "Convert to GUID reference")] public void TestAsmRef01() { DoNamedTest("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class ConvertToGuidReferenceQuickFixTests : QuickFixTestBase<ConvertToGuidReferenceQuickFix>
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\ConvertToGuidReference";

        [Test, UnityPluginBackendChecklist("Assembly definition files", "GUID references", "Convert to GUID reference")] public void Test01() { DoNamedTest("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
        [Test, ExecuteScopedActionInFile, UnityPluginBackendChecklist("Assembly definition files", "GUID references", "Convert to GUID reference")] public void TestExecuteInScope() { DoNamedTest("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }

        [Test, TestFileExtension(".asmref"), UnityPluginBackendChecklist("Assembly definition files", "GUID references", "Convert to GUID reference")] public void TestAsmRef01() { DoNamedTest("GuidReference_SecondProject.asmdef", "GuidReference_SecondProject.asmdef.meta"); }
    }
}
