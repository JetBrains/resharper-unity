using JetBrains.ReSharper.FeaturesTestFramework.Intentions;

namespace JetBrains.ReSharper.Plugins.Unity.Tests.CSharp.Intentions.QuickFixes
{
    public class QuickFixAfterSwaAvailabilityTestBase : QuickFixAvailabilityTestBase
    {
        protected override bool ShouldGlobalWarnings => true;
    }
}