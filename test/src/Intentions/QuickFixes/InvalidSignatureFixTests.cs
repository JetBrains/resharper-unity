using JetBrains.ReSharper.FeaturesTestFramework.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Feature.Services.QuickFixes;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.Intentions.QuickFixes
{
    [TestUnity]
    public class InvalidSignatureFixAvailabilityTests : QuickFixAvailabilityTestBase
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\InvalidSignature\Availability";

        [Test] public void AddAllParameters() { DoNamedTest(); }
        [Test] public void AddMissingParameter() { DoNamedTest(); }
        [Test] public void RemoveAllParameters() { DoNamedTest(); }
        [Test] public void ReorderParameters() { DoNamedTest(); }
    }

    [TestUnity]
    public class InvalidSignatureFixTests : CSharpQuickFixTestBase<InvalidSignatureFix>
    {
        protected override string RelativeTestDataPath=> @"Intentions\QuickFixes\InvalidSignature";

        [Test] public void AddAllParameters() { DoNamedTest(); }
        [Test] public void AddMissingParameter() { DoNamedTest(); }
        [Test] public void RemoveAllParameters() { DoNamedTest(); }
        [Test] public void RenameParameters() { DoNamedTest(); }
        [Test] public void ReorderParameters() { DoNamedTest(); }
    }
}