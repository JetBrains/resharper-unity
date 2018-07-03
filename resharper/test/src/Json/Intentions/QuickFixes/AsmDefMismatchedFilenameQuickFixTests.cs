using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Json.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.Json.Feature.Services.QuickFixes;
using JetBrains.ReSharper.TestFramework;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Json.Intentions.QuickFixes
{
    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefMismatchedFilenameQuickFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath => @"Json\Intentions\QuickFixes\MismatchedFilename\Availability";

        [Test] public void Test01() { DoNamedTest(); }
    }

    [TestUnity]
    [TestFileExtension(".asmdef")]
    public class AsmDefMismatchedFilenameQuickFixTests : QuickFixTestBase<AsmDefRenameFileToMatchAssemblyNameQuickFix>
    {
        protected override string RelativeTestDataPath => @"Json\Intentions\QuickFixes\MismatchedFilename";

        [Test] public void Test01() { DoNamedTest(); }
    }
}