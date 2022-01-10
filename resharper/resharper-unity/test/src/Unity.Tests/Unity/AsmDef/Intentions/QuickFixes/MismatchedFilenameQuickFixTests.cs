using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Tests.Unity.AsmDef.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class MismatchedFilenameQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\MismatchedFilename\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class MismatchedFilenameQuickFixTests : QuickFixTestBase<RenameFileToMatchAssemblyNameQuickFix>
    {
        protected override string RelativeTestDataPath => @"AsmDef\Intentions\QuickFixes\MismatchedFilename";

        [Test] public void Test01() { DoNamedTest(); }
    }
}
